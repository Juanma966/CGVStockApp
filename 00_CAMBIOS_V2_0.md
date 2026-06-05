# 📋 DOCUMENTO DE CAMBIOS - CGVStockApp v2.0
## Arquitectura Revisada | Decisiones y Justificaciones

**Fecha:** Junio 2026  
**Versión:** 2.0 (Revisada)  
**Estado:** FINAL

---

## 📊 RESUMEN EJECUTIVO DE CAMBIOS

| Aspecto | v1.0 | v2.0 | Razón |
|---------|------|------|-------|
| **Acceso a Datos** | Repository + UnitOfWork | IApplicationDbContext directo | EF Core ya es abstracción suficiente en CQRS |
| **AutoMapper** | En todas partes | Solo comandos complejos | Proyecciones Select() son más claras y eficientes |
| **Proyecciones Query** | AutoMapper | Select() explícito | Mejor performance, legibilidad |
| **AccountingMovement** | Planificado | ✅ Mantener | Requerido para "Libro Diario" |
| **ProductCode** | Sin cambios | ✅ Sin cambios | Mantener implementación |
| **Autenticación** | Custom User + JWT | ✅ Custom User + JWT | Más ágil que Identity para 50 días |
| **CQRS Conceptos** | C/R/U/D | Corregido a Commands/Queries | Alineación correcta |

---

## 🔄 CAMBIO 1: ELIMINACIÓN DE REPOSITORY PATTERN

### ❌ LO QUE SE ELIMINA

```
Infrastructure/
├── Repositories/
│   ├── IRepository.cs           ❌ ELIMINAR
│   ├── Repository.cs            ❌ ELIMINAR
│   ├── IProductRepository.cs     ❌ ELIMINAR
│   ├── ProductRepository.cs      ❌ ELIMINAR
│   ├── ISalesRepository.cs       ❌ ELIMINAR
│   ├── SalesRepository.cs        ❌ ELIMINAR
│   ├── IUnitOfWork.cs            ❌ ELIMINAR
│   └── UnitOfWork.cs             ❌ ELIMINAR
```

### ✅ LO QUE SE AGREGA

```
Application/
├── Common/
│   └── Interfaces/
│       └── IApplicationDbContext.cs    ✅ NUEVO

Infrastructure/
└── Persistence/
    └── Context/
        └── ApplicationDbContext.cs     (sin cambios, pero implementa IApplicationDbContext)
```

### 🎯 RAZÓN TÉCNICA

En arquitectura CQRS:
- **Ya hay separación de responsabilidades** (Command ≠ Query)
- **EF Core IS la abstracción** (DbSet<T> es un repositorio)
- **Repository Pattern + EF Core = doble abstracción** (innecesaria)
- **Cada Handler es responsable** de su propia lógica de acceso a datos

**Comparación:**

```csharp
// v1.0 (Repository Pattern - 3 capas de abstracción)
Handler → UnitOfWork → Repository → DbContext → BD

// v2.0 (CQRS + EF Core directo - 2 capas)
Handler → DbContext → BD
```

### 📝 PATRÓN RESULTANTE

```csharp
// En Application/Common/Interfaces/
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<Sale> Sales { get; }
    DbSet<User> Users { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// En Infrastructure/Persistence/Context/
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    // Implementar DbSets y SaveChangesAsync
}

// En Handler
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly IApplicationDbContext _context;
    
    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product { ... };
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        // ...
    }
}
```

### ✅ VENTAJAS

- ✅ Menos código (no hay Repository abstracto)
- ✅ Más claro (DbSet es familiar para todos)
- ✅ EF Core features disponibles (LINQ directo)
- ✅ Fácil testing (Mock IApplicationDbContext)
- ✅ CQRS ya proporciona separación

### ⚠️ TRADE-OFF

- ⚠️ Menos testable que con Repository (pero acceptable en CQRS)
- ⚠️ Acoplamiento a EF Core (pero es la realidad en .NET)

---

## 🗺️ CAMBIO 2: AUTOMAPPER LIMITADO + SELECT() EN QUERIES

### ❌ ANTES (v1.0)

```csharp
// Todos los mappings con AutoMapper (redundante)

// Query Handler
var products = await _context.Products
    .Include(p => p.Category)
    .ToListAsync();

return _mapper.Map<List<ProductResponse>>(products);  // ❌ AutoMapper innecesario
```

### ✅ AHORA (v2.0)

```
AutoMapper Aceptado En:
├── Commands complejos (CreateProductCommand → Product)
│   └── Transformaciones con lógica
│
Select() Explícito En:
├── Queries (ProductResponse)
│   └── Proyecciones SELECT SQL
```

### 📝 PATRONES

#### **1. COMANDO COMPLEJO (CON AutoMapper)**

```csharp
// CreateProductCommand → Product (requiere transformación)
public class CreateProductMappingProfile : Profile
{
    public CreateProductMappingProfile()
    {
        CreateMap<CreateProductCommand, Product>()
            .ForMember(dest => dest.ProductCode, 
                opt => opt.Ignore())  // Se genera en el handler
            .ForMember(dest => dest.AvailableStock,
                opt => opt.MapFrom(src => src.InitialStock));
    }
}

// En Handler
var product = _mapper.Map<Product>(command);
product.ProductCode = GenerateProductCode();
_context.Products.Add(product);
```

#### **2. QUERY SIMPLE (CON Select() - SIN AutoMapper)**

```csharp
// Query simple: no necesita AutoMapper
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductResponse>>
{
    private readonly IApplicationDbContext _context;
    
    public async Task<List<ProductResponse>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                Name = p.Name,
                Description = p.Description,
                PublicPrice = p.PublicPrice,
                WholesalePrice = p.WholesalePrice,
                AvailableStock = p.AvailableStock,
                CategoryName = p.Category.Name,
                SubcategoryName = p.Subcategory.Name
            })
            .ToListAsync(cancellationToken);
    }
}
```

#### **3. QUERY COMPLEJA (CON Select() y Lógica)**

```csharp
// Dashboard: Calcula el total en la query
public class GetDailyDashboardQueryHandler : IRequestHandler<GetDailyDashboardQuery, DashboardResponse>
{
    private readonly IApplicationDbContext _context;
    
    public async Task<DashboardResponse> Handle(
        GetDailyDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var startOfDay = request.Date.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);
        
        var movements = await _context.AccountingMovements
            .Where(m => m.MovementDate >= startOfDay && m.MovementDate <= endOfDay)
            .Select(m => new MovementDto
            {
                Date = m.MovementDate,
                Type = m.Type.ToString(),
                Description = m.Description,
                Amount = m.Amount,
                IsIncome = m.IsIncome
            })
            .ToListAsync(cancellationToken);
        
        return new DashboardResponse
        {
            Period = "Diario",
            Date = request.Date,
            TotalIncomes = movements.Where(m => m.IsIncome).Sum(m => m.Amount),
            TotalExpenses = movements.Where(m => !m.IsIncome).Sum(m => m.Amount),
            Movements = movements
        };
    }
}
```

### ✅ VENTAJAS

- ✅ **Performance:** Select() genera SQL directo (sin materializar objetos)
- ✅ **Claridad:** Se ve exactamente qué datos se traen
- ✅ **Menos dependencias:** Menos uso de AutoMapper
- ✅ **Debugging:** Fácil ver la proyección

### 📊 COMPARACIÓN

| Escenario | Solución | Por qué |
|-----------|----------|--------|
| Command → Entity (CREATE/UPDATE) | AutoMapper | Logica de transformación |
| Entity → DTO (lectura simple) | Select() | Mejor performance, claridad |
| Lectura con cálculos | Select() + LINQ | Lógica en BD = más rápido |

---

## 💼 CAMBIO 3: ACCOUNTINGMOVEMENT - MANTENER ✅

### 📌 REQUISITO ORIGINAL

```
"Dashboard donde verá las ventas y las compras que haga 
en forma de tabla de libro diario que en parte de entrada 
se registrarán solo las ventas que haga y en parte salida 
lo que gaste en compras de mercadería y en pagos extra"
```

### ✅ DECISIÓN: MANTENER TABLA SEPARADA

```csharp
// Domain/Entities/AccountingMovement.cs
public class AccountingMovement : AuditableEntity
{
    public int Id { get; set; }
    public DateTime MovementDate { get; set; }
    public AccountingMovementType Type { get; set; }  // Sale, Purchase, Expense
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsIncome { get; set; }  // true = entrada, false = salida
    
    public int? SaleId { get; set; }
    public Sale Sale { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; }
}

// Domain/Enums/AccountingMovementType.cs
public enum AccountingMovementType
{
    Sale = 1,      // Venta registrada
    Purchase = 2,  // Compra de mercadería
    Expense = 3    // Gastos extra
}
```

### 🎯 RAZÓN TÉCNICA

1. **Requisito explícito:** Dashboard requiere "libro diario"
2. **Separación de responsabilidades:** 
   - Sales = ventas al cliente
   - AccountingMovements = registros contables
3. **Auditoría:** Cada movimiento financiero queda registrado
4. **Flexibilidad:** Futuro: reportes contables, exportación, etc.

### 📊 FLUJO DE DATOS

```
Venta registrada (CreateSaleCommand)
    ├─ Crear Sale
    ├─ Descontar Stock
    └─ Crear AccountingMovement (IsIncome = true, Amount = total)

Gasto registrado (CreateExpenseCommand)  // Feature futura
    └─ Crear AccountingMovement (IsIncome = false, Amount = monto)

Dashboard Query
    └─ Lee AccountingMovements
    └─ Agrupa por periodo
    └─ Suma incomes vs expenses
```

### ✅ NO AFECTA CRONOGRAMA

- ✅ 1 tabla adicional
- ✅ 1 migración más
- ✅ Lógica simple (se crea automáticamente al vender)
- ✅ Ya está en roadmap (Fase 4, Subproducto 5)

---

## 🆔 CAMBIO 4: PRODUCTCODE - SIN CAMBIOS ✅

### ✅ MANTENER COMO ESTÁ

```csharp
// Formato: AANNNN
// A = Primera letra de Categoría
// A = Primera letra de Subcategoría
// NNNN = Número secuencial (0001, 0002, ...)

// Ejemplo: FL0001
// F = Frutas (Categoría)
// L = Limones (Subcategoría)
// 0001 = Primer producto en esa combinación
```

### 🔧 AGREGAR VALIDACIÓN EN BD

```sql
-- En Migration:
ALTER TABLE products 
ADD CONSTRAINT uk_product_code UNIQUE (product_code);
```

### ✅ GENERAR EN HANDLER

```csharp
public async Task<ProductResponse> Handle(
    CreateProductCommand request,
    CancellationToken cancellationToken)
{
    // Obtener letras
    var categoryLetter = request.CategoryId.HasValue 
        ? (await _context.Categories.FindAsync(...)).Name[0].ToString().ToUpper()
        : "P";
    
    var subcategoryLetter = request.SubcategoryId.HasValue 
        ? (await _context.Subcategories.FindAsync(...)).Name[0].ToString().ToUpper()
        : "0";
    
    // Obtener próximo número
    var lastNumber = await _context.Products
        .Where(p => p.ProductCode.StartsWith(categoryLetter + subcategoryLetter))
        .OrderByDescending(p => p.ProductCode)
        .Select(p => int.Parse(p.ProductCode.Substring(2)))
        .FirstOrDefaultAsync(cancellationToken) ?? 0;
    
    var productCode = $"{categoryLetter}{subcategoryLetter}{lastNumber + 1:D4}";
    
    var product = new Product
    {
        ProductCode = productCode,
        // ...
    };
    
    _context.Products.Add(product);
    await _context.SaveChangesAsync(cancellationToken);
    
    return new ProductResponse { ProductCode = productCode, ... };
}
```

---

## 🔐 CAMBIO 5: AUTENTICACIÓN - CUSTOM USER + JWT ✅

### ✅ MANTENER CUSTOM (SIN ASP.NET CORE IDENTITY)

**Razón:** Identity es más complejo (requiere 5+ tablas y setup adicional). Con 50 días FIRMES, custom + JWT es más ágil.

### 📝 ESTRUCTURA FINAL

```csharp
// Domain/Entities/User.cs
public class User : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;
}

// Domain/Entities/Role.cs
public class Role : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; }  // "Admin", "Seller"
    public ICollection<User> Users { get; set; }
}

// Infrastructure/Services/PasswordHashService.cs
public class PasswordHashService
{
    public string HashPassword(string password)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("salt")))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hash);
        }
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("salt")))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(computedHash) == hash;
        }
    }
}

// Infrastructure/Services/JwtTokenService.cs
public class JwtTokenService
{
    public string GenerateToken(User user, string secretKey, int expirationMinutes)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secretKey);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("role", user.Role.Name)
            }),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
```

### ✅ VENTAJAS

- ✅ Más rápido de implementar
- ✅ Menos tablas
- ✅ Fácil customizar
- ✅ JWT integrado sin complejidad

### ⚠️ TRADE-OFF

- ⚠️ Sin 2FA (no requerido en requisitos)
- ⚠️ Sin password reset automático (implementable como feature futura)

---

## 🎯 CORRECCIÓN 6: CQRS - CONCEPTOS

### ❌ INCORRECTO (v1.0)

```
Commands = C/R/U/D
Queries = G
```

### ✅ CORRECTO (v2.0)

```
COMMANDS = Escriben/Modifican estado:
├── Create        (Crear entidad)
├── Update        (Modificar entidad)
├── Delete        (Eliminar entidad)
└── Acciones      (Operaciones de negocio que cambian estado)
                  Ejemplo: "TransferStockCommand", "ApplyPriceIncreaseCommand"

QUERIES = Leen sin modificar estado:
├── GetAll        (Obtener listado)
├── GetById       (Obtener por ID)
├── Search        (Buscar)
├── GetByPeriod   (Obtener por rango)
└── Calculate     (Calcular datos sin persistir)
                  Ejemplo: "GetDashboardQuery", "GetMinimumStockQuery"
```

### 📌 DIFERENCIA CLAVE

```csharp
// ❌ INCORRECTO
public class GetProductsQuery : IRequest<List<ProductResponse>>  // NO es una "R"
{
    // ...
}

// ✅ CORRECTO
/// Obtiene listado de productos sin modificar estado
public class GetProductsQuery : IRequest<List<ProductResponse>>
{
    // ...
}
```

### ✅ ACTUALIZAR EN TODA DOCUMENTACIÓN

- Todos los Commands crean/modifican/eliminan/cambian estado
- Todas las Queries solo leen
- Los nombres reflejan la intención

---

## 📊 IMPACTO EN CRONOGRAMA

| Cambio | Impacto en Tiempo | Impacto en Complejidad |
|--------|------------------|----------------------|
| Eliminar Repository | **-2 días** (menos código) | Reduce 1 capa |
| AutoMapper limitado | **-1 día** (menos mappings) | Reduce dependencias |
| AccountingMovement | **Sin cambio** (ya planeado) | Mismo esfuerzo |
| ProductCode | **Sin cambio** | Mismo |
| Custom + JWT | **Sin cambio** | Mismo |
| CQRS corrección | **Sin cambio** (conceptual) | Claridad |
| **TOTAL** | **-3 días** ✅ | Más limpio |

### ✅ NUEVO CRONOGRAMA

- Antes: 45-50 días
- Ahora: **42-47 días** (3 días ganados)
- **Contingencia:** Usar tiempo ganado para testing, documentación, o adelantos

---

## 🎯 CONCLUSIÓN DE CAMBIOS

| Cambio | Decisión | Justificación |
|--------|----------|---|
| Repository Pattern | ❌ ELIMINAR | EF Core + CQRS ya es suficiente |
| AutoMapper en todo | ❌ LIMITAR a Commands | Select() en Queries es más eficiente |
| AccountingMovement | ✅ MANTENER | Requerido para "Libro Diario" |
| ProductCode | ✅ MANTENER | Sin cambios + UNIQUE constraint |
| ASP.NET Identity | ❌ NO USAR | Custom + JWT más ágil en 50 días |
| CQRS Conceptos | ✅ CORREGIR | Commands ≠ CRUD, actualizar referencias |

### 📌 IMPACTO FINAL

- ✅ **Arquitectura:** Más limpia, menos capas
- ✅ **Cronograma:** -3 días (ganados para testing/refinamiento)
- ✅ **Funcionalidad:** 100% de requisitos mantenidos
- ✅ **Calidad:** Más clara y enfocada

---

**Próximo paso:** Reescribir documentación con estos cambios aplicados.

Documentos nuevos:
1. ✏️ Arquitectura Revisada v2.0
2. ✏️ Implementación Revisada v2.0
3. ✏️ Roadmap Revisado v2.0

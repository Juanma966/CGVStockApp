# 📌 ROADMAP EJECUTIVO - CGVStockApp CQRS
## Resumen de Decisiones Arquitectónicas y Plan de Ejecución

---

## 🎯 RESUMEN EJECUTIVO

### Proyecto: **CGVStockApp**
- **Objetivo:** Sistema integral de gestión de ventas y control de stock
- **Arquitectura:** Clean Architecture + CQRS + MediatR
- **Duración:** 45-50 días (FIRME)
- **Stack:** .NET 9 + PostgreSQL + React
- **Lenguaje:** Código en inglés, documentación en español

### Cambios vs Arquitectura Original
| Aspecto | Original | CQRS Actual |
|---------|----------|-------------|
| **Patrón** | Services Tradicionales | Commands/Queries + Handlers |
| **Validación** | Data Annotations | FluentValidation |
| **Organización** | Por capa | Por Feature (dominio) |
| **Testing** | Difícil | Fácil (handlers independientes) |
| **Escalabilidad** | Media | Alta |

---

## 📊 ROADMAP DETALLADO (45-50 DÍAS)

### **FASE 0: SETUP INICIAL (Días 1-3) | 15 horas**

**Objetivo:** Ambiente completamente configurado y listo

#### Día 1: Instalación Base
- [ ] Descargar e instalar .NET 9 SDK (actual tienes 8)
- [ ] Instalar PostgreSQL 16 + pgAdmin
- [ ] Instalar VSCode + extensiones C#
- [ ] Comando: `dotnet --version` → debe mostrar 9.0.x

#### Día 2: Crear Solución
- [ ] Crear estructura: `dotnet new sln -n CGVStockApp`
- [ ] Crear 4 proyectos:
  - CGVStockApp.Domain (classlib)
  - CGVStockApp.Application (classlib)
  - CGVStockApp.Infrastructure (classlib)
  - CGVStockApp.Api (webapi)
- [ ] Agregar referencias entre proyectos
- [ ] Instalar paquetes NuGet principales

#### Día 3: Verificación y Git
- [ ] Comando: `dotnet build` → sin errores
- [ ] Crear repositorio GitHub
- [ ] Commit inicial
- [ ] Estructura de carpetas lista

#### ✅ Checklist Fase 0
- [ ] .NET 9 instalado
- [ ] PostgreSQL + pgAdmin funcionando
- [ ] Solución con 4 proyectos creada
- [ ] Referencias correctas entre proyectos
- [ ] `dotnet build` sin errores
- [ ] Repositorio GitHub creado

---

### **FASE 1: BASE DE DATOS (Días 4-9) | 30 horas**

**Objetivo:** Modelos de dominio y BD estructura lista

#### Estructura Domain Layer
```
Domain/
├── Entities/
│   ├── User.cs
│   ├── Role.cs
│   ├── Category.cs
│   ├── Subcategory.cs
│   ├── Product.cs          ← MÁS IMPORTANTE
│   ├── Sale.cs
│   ├── SaleDetail.cs
│   ├── StockMovement.cs
│   └── AccountingMovement.cs
│
├── Enums/
│   ├── RoleType.cs
│   ├── PaymentMethodType.cs
│   ├── CustomerType.cs
│   ├── StockMovementType.cs
│   └── AccountingMovementType.cs
│
├── Common/
│   ├── AuditableEntity.cs
│   └── IAggregateRoot.cs
│
└── Exceptions/
    ├── DomainException.cs
    ├── InvalidProductException.cs
    └── InsufficientStockException.cs
```

#### Día 4-5: Entidades Base
- [ ] AuditableEntity (CreatedAt, ModifiedAt, CreatedBy, ModifiedBy)
- [ ] IAggregateRoot interface
- [ ] Enums: RoleType, CustomerType, PaymentMethodType
- [ ] Excepciones de dominio

#### Día 6: Entidades Principales
- [ ] User + Role
- [ ] Category + Subcategory
- [ ] Product (con lógica de dominio: DecreaseStock, UpdatePrices)
- [ ] Sale + SaleDetail

#### Día 7: Entidades Complementarias
- [ ] StockMovement
- [ ] AccountingMovement (para dashboard/libro diario)

#### Día 8: DbContext + Configurations
- [ ] StockAppDbContext en Infrastructure/Persistence/Context
- [ ] IEntityTypeConfiguration<T> para cada entidad
- [ ] Validaciones fluent en configuraciones

#### Día 9: Migraciones
- [ ] Primera migración: `dotnet ef migrations add InitialCreate`
- [ ] Actualizar BD: `dotnet ef database update`
- [ ] Seeding de roles (Admin, Vendedor)
- [ ] Verificar en pgAdmin que se crearon todas las tablas

#### ✅ Checklist Fase 1
- [ ] Todas las entidades en Domain
- [ ] DbContext configurado correctamente
- [ ] EntityTypeConfigurations para cada entidad
- [ ] Migraciones ejecutadas
- [ ] Base de datos con todas las tablas
- [ ] Roles "Admin" y "Vendedor" creados en BD

---

### **FASE 2: INFRASTRUCTURE (Días 10-12) | 15 horas**

**Objetivo:** Acceso a datos listo (Repositories, UnitOfWork)

#### Día 10: Repository Pattern
- [ ] IRepository<T> interface genérica
- [ ] Repository<T> implementación base
- [ ] Métodos: GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync
- [ ] IQueryable<T> GetAll() para queries complejas

#### Día 11: UnitOfWork + Específicos
- [ ] IUnitOfWork interface
- [ ] UnitOfWork implementation
- [ ] Propiedades: Products, Categories, Sales, etc.
- [ ] SaveChangesAsync()
- [ ] Repositorios específicos si hay lógica especial:
  - IProductRepository: búsqueda, stock mínimo
  - ISalesRepository: por período

#### Día 12: Services + DI
- [ ] JwtTokenService (generación de JWT)
- [ ] PasswordHashService (hash seguro)
- [ ] PdfGenerationService (iText7)
- [ ] DependencyInjection.cs en Infrastructure
- [ ] Registrar todo en Program.cs

#### ✅ Checklist Fase 2
- [ ] Repository pattern implementado
- [ ] UnitOfWork funcionando
- [ ] JwtTokenService generando tokens válidos
- [ ] DependencyInjection registrando correctamente

---

### **FASE 3: APPLICATION - STRUCTURE (Días 13-15) | 15 horas**

**Objetivo:** Carpetas y pipelines CQRS listos

#### Día 13: Common Base
- [ ] Common/Behaviors/ValidationBehavior.cs
- [ ] Common/Behaviors/LoggingBehavior.cs
- [ ] Common/Behaviors/PerformanceBehavior.cs
- [ ] Common/Exceptions/ApplicationException.cs
- [ ] Common/Exceptions/ValidationException.cs
- [ ] Common/Interfaces/IRequest.cs

#### Día 14: Feature Folders
- [ ] Crear carpeta Features/
- [ ] Subcarpetas para cada feature:
  - Authentication/
  - Products/
  - Categories/
  - Subcategories/
  - Sales/
  - Users/
  - Roles/
  - Dashboard/

#### Día 15: DependencyInjection
- [ ] Application/DependencyInjection.cs
- [ ] Registrar MediatR
- [ ] Registrar FluentValidation
- [ ] Registrar AutoMapper
- [ ] Registrar Pipeline Behaviors

#### ✅ Checklist Fase 3
- [ ] Carpeta Features con todas las subcarpetas
- [ ] Common/ implementado
- [ ] DependencyInjection.cs completado

---

### **FASE 4: CQRS FEATURES - IMPLEMENTACIÓN (Días 16-35) | 80 horas**

**Objetivo:** Todas las features CRUD en CQRS

#### Subproducto 1: Authentication (Días 16-17) | 10 horas
**Features/Authentication/**
- [ ] Commands:
  - LoginCommand
- [ ] Handlers:
  - LoginCommandHandler (genera JWT)
- [ ] DTOs:
  - LoginRequest
  - LoginResponse
- [ ] Validators:
  - LoginCommandValidator

#### Subproducto 2: Products (Días 18-24) | 35 horas
**Features/Products/**

**Commands:**
- [ ] CreateProductCommand
- [ ] UpdateProductCommand
- [ ] DeleteProductCommand
- [ ] UpdatePriceCommand (modificación porcentual)

**Handlers:**
- [ ] CreateProductCommandHandler
  - Genera ProductCode automático (AANNNN)
  - Validaciones de categoría/subcategoría
  - Persistencia
- [ ] UpdateProductCommandHandler
- [ ] DeleteProductCommandHandler
- [ ] UpdatePriceCommandHandler
  - Aplica porcentaje a todos los productos de una categoría
  - Afecta: PublicPrice y WholesalePrice

**Queries:**
- [ ] GetProductsQuery (con filtros)
- [ ] GetProductByIdQuery
- [ ] SearchProductsQuery (búsqueda por nombre)
- [ ] GetMinimumStockProductsQuery (para reporte)

**QueryHandlers:**
- [ ] Correspondientes a cada query
- [ ] Orden alfabético: Categoría → Subcategoría → Producto

**DTOs:**
- [ ] CreateProductRequest
- [ ] UpdateProductRequest
- [ ] ProductResponse (con category/subcategory names)

**Validators:**
- [ ] CreateProductCommandValidator
- [ ] UpdateProductCommandValidator

**Mappings:**
- [ ] ProductMappingProfile (AutoMapper)

#### Subproducto 3: Categories (Días 25-27) | 15 horas
**Features/Categories/**
- [ ] CRUD completo (C/R/U/D)
- [ ] Queries con subcategorías
- [ ] DTOs específicos
- [ ] Validators
- [ ] Mappings

#### Subproducto 4: Subcategories (Días 28-29) | 10 horas
**Features/Subcategories/**
- [ ] CRUD completo
- [ ] Query para obtener por categoría
- [ ] Validar FK a Category

#### Subproducto 5: Sales (Días 30-33) | 20 horas
**Features/Sales/**

**Commands:**
- [ ] CreateSaleCommand (MÁS COMPLEJA)
  - Acepta lista de productos y cantidades
  - Valida stock disponible
  - Reduce AvailableStock automáticamente
  - Calcula precio según CustomerType (Final/Mayorista)
  - Registra movimiento de stock
  - Registra en libro diario (AccountingMovement)

**Handlers:**
- [ ] CreateSaleCommandHandler
  - Lógica compleja: validar stock, descontar, registrar

**Queries:**
- [ ] GetSalesQuery (con filtros)
- [ ] GetSaleByIdQuery
- [ ] GetSalesByPeriodQuery (para dashboard)

**DTOs:**
- [ ] CreateSaleRequest
- [ ] SaleResponse
- [ ] SaleDetailResponse

**Validators:**
- [ ] CreateSaleCommandValidator (incluyendo detalles)

#### Subproducto 6: Users (Días 34-35) | 10 horas
**Features/Users/**
- [ ] CRUD completo
- [ ] UpdatePasswordCommand
- [ ] Validaciones de unicidad (username)
- [ ] Hash seguro de contraseña

#### ✅ Checklist Fase 4
- [ ] Todas las features en Features/
- [ ] Commands, Handlers, Queries implementados
- [ ] DTOs por feature
- [ ] Validators en cada feature
- [ ] Mappings automapper por feature
- [ ] Repositorios específicos si necesarios
- [ ] Lógica de dominio en handlers
- [ ] ProductCode generación automática
- [ ] Stock deduction funcional
- [ ] Libro diario registrando movimientos

---

### **FASE 5: API CONTROLLERS (Días 36-38) | 15 horas**

**Objetivo:** Endpoints HTTP para consumir CQRS

#### Estructura Controllers
```
Api/Controllers/
├── AuthController.cs
├── ProductController.cs
├── CategoryController.cs
├── SubcategoryController.cs
├── SalesController.cs
├── UserController.cs
└── DashboardController.cs
```

#### Día 36: ProductController
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductController
{
    [HttpPost("create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct(CreateProductRequest request)
    {
        var command = new CreateProductCommand { ... };
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] int? categoryId)
    {
        var query = new GetProductsQuery { CategoryId = categoryId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        var query = new SearchProductsQuery { SearchTerm = term };
        return Ok(await _mediator.Send(query));
    }
    
    [HttpGet("minimum-stock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetMinimumStock()
    {
        var query = new GetMinimumStockProductsQuery();
        var products = await _mediator.Send(query);
        return Ok(products);
    }
    
    [HttpGet("minimum-stock/pdf")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DownloadMinimumStockPdf()
    {
        var query = new GetMinimumStockProductsQuery();
        var products = await _mediator.Send(query);
        var pdf = _pdfService.GeneratePdf(products);
        return File(pdf, "application/pdf", $"stock-minimo-{DateTime.Now:yyyyMMdd}.pdf");
    }
}
```

#### Día 37: SalesController + UserController
- [ ] SalesController con CreateSale
- [ ] UserController con CRUD
- [ ] AuthController con Login

#### Día 38: DashboardController + Error Handling
- [ ] DashboardController con queries diarias/semanales/mensuales
- [ ] ErrorHandlingMiddleware centralizado
- [ ] Manejo de excepciones (ValidationException, NotFoundException)

#### ✅ Checklist Fase 5
- [ ] Todos los controllers creados
- [ ] Endpoints mapeados a Commands/Queries
- [ ] [Authorize] correctamente en endpoints
- [ ] Roles (Admin, Seller) validando
- [ ] Errores manejados correctamente

---

### **FASE 6: FRONTEND REACT (Días 39-42) | 20 horas**

**Objetivo:** Interfaz completa lista

#### Día 39: Setup React
- [ ] `npx create-react-app frontend`
- [ ] Instalar axios, react-router-dom, zustand
- [ ] Estructura de carpetas

#### Día 40: Login + Rutas Protegidas
- [ ] Login funcional (obtiene JWT)
- [ ] Almacenar token en localStorage (Zustand)
- [ ] PrivateRoute wrapper
- [ ] Navbar con rol

#### Día 41: Vistas Admin
- [ ] Producto CRUD
- [ ] Categoría CRUD
- [ ] Dashboard (tabla de movimientos)
- [ ] Stock mínimo con botón PDF

#### Día 42: Vistas Vendedor + Modal
- [ ] Menu de productos (search + tabla)
- [ ] Modal de venta (producto, cantidad, cliente, pago)
- [ ] Validación stock insuficiente
- [ ] Mensaje de "Venta registrada"

#### ✅ Checklist Fase 6
- [ ] React app conectada a API
- [ ] Login funcional
- [ ] CRUD de productos
- [ ] Modal de venta con validaciones
- [ ] Dashboard mostrando movimientos
- [ ] PDF descargable
- [ ] Responsivo (mobile/tablet/PC)

---

### **FASE 7: TESTING & DEPLOYMENT (Días 43-50) | 25 horas**

#### Días 43-45: Testing Manual
- [ ] Checklist completo de funcionalidades
- [ ] Correcciones de bugs
- [ ] Testing en diferentes navegadores
- [ ] Testing en móvil/tablet

#### Días 46-48: Deployment
- [ ] Setup en Railway.app (recomendado)
- [ ] Configurar variables de entorno
- [ ] BD en producción
- [ ] Primer deploy

#### Días 49-50: Ajustes Finales
- [ ] Monitoreo en producción
- [ ] Fixes de último momento
- [ ] Documentación de usuario final

#### ✅ Checklist Fase 7
- [ ] Todas funcionalidades testeadas
- [ ] Deployment exitoso en Railway
- [ ] BD producción funcionando
- [ ] App accesible desde URL pública

---

## 🏗️ ESTRUCTURA FINAL DE CARPETAS

```
CGVStockApp/
│
├── CGVStockApp.sln
│
├── CGVStockApp.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── Common/
│   ├── Exceptions/
│   └── CGVStockApp.Domain.csproj
│
├── CGVStockApp.Application/
│   ├── Features/
│   │   ├── Authentication/
│   │   ├── Products/
│   │   ├── Categories/
│   │   ├── Subcategories/
│   │   ├── Sales/
│   │   ├── Users/
│   │   ├── Roles/
│   │   └── Dashboard/
│   ├── Common/
│   │   ├── Behaviors/
│   │   ├── Exceptions/
│   │   ├── Interfaces/
│   │   └── Constants/
│   ├── DependencyInjection.cs
│   └── CGVStockApp.Application.csproj
│
├── CGVStockApp.Infrastructure/
│   ├── Persistence/
│   │   ├── Context/
│   │   ├── Configurations/
│   │   └── Migrations/
│   ├── Repositories/
│   ├── Services/
│   ├── DependencyInjection.cs
│   └── CGVStockApp.Infrastructure.csproj
│
├── CGVStockApp.Api/
│   ├── Controllers/
│   ├── Middlewares/
│   ├── Program.cs
│   ├── appsettings.json
│   └── CGVStockApp.Api.csproj
│
└── frontend/
    ├── src/
    │   ├── pages/
    │   ├── components/
    │   ├── services/
    │   ├── store/
    │   └── App.jsx
    └── package.json
```

---

## 📚 FUNCIONALIDADES FINALES CHECKLIST

### Admin
- [ ] Crear/Editar/Eliminar Usuarios
- [ ] Crear/Editar/Eliminar Categorías
- [ ] Crear/Editar/Eliminar Subcategorías
- [ ] Crear/Editar/Eliminar Productos
- [ ] Modificar Precios (por porcentaje)
- [ ] Ver Stock Mínimo (tabla + PDF descargable)
- [ ] Dashboard (movimientos diarios/semanales/mensuales)
- [ ] Ver Menú de Productos

### Vendedor
- [ ] Ver Menú de Productos (búsqueda + tabla)
- [ ] Registrar Venta (modal)
  - Seleccionar cliente (Final/Mayorista)
  - Seleccionar forma de pago
  - Agregar productos (con autocompletado)
  - Validar stock
  - Mensaje de "Venta registrada"

### Ambos
- [ ] Autenticación JWT
- [ ] Menu de productos principal con búsqueda

---

## 🔑 PUNTOS CRÍTICOS A NO OLVIDAR

1. **ProductCode Auto-generación**
   - Formato: AANNNN (primera letra categoría + primera letra subcategoría + número)
   - Se genera automáticamente en CreateProductCommandHandler
   - Se muestra solo en menú de productos

2. **Stock Management**
   - StockInicial: nunca cambia
   - AvailableStock: se actualiza con cada venta
   - Formula: AvailableStock = StockInicial - cantidad_vendida
   - Validación: No permitir venta si AvailableStock < cantidad

3. **Precios Dinámicos**
   - Modificación es FUTURA (no retroactiva)
   - Se aplica a todos los productos de una categoría/subcategoría
   - Usando porcentaje: +15% o -10%
   - Afecta: PublicPrice y WholesalePrice

4. **Libro Diario**
   - ENTRADA: ventas realizadas
   - SALIDA: compras de mercadería + gastos
   - Tabla con fecha, tipo, concepto, monto, entrada/salida
   - Vistas: Diaria, Semanal, Mensual, Total

5. **Flujo CQRS**
   - Commands: C/R/U/D (escriben)
   - Queries: G (leen)
   - Handlers: ejecutan la lógica
   - Validators: FluentValidation
   - Mappings: AutoMapper por feature

---

## 🚀 ORDEN DE EJECUCIÓN RECOMENDADO

Para máxima eficiencia:

1. **Domain entities** primero (Día 4-5)
2. **DbContext + Configurations** (Día 8)
3. **Migraciones** (Día 9)
4. **UnitOfWork + Repositories** (Día 10-11)
5. **Features Implementation** (Día 16-35) - EN ESTE ORDEN:
   - Authentication (simple, necesario para rest)
   - Products (más usada)
   - Sales (más compleja)
   - Dashboard (queries simples)
6. **Controllers** (Día 36-38)
7. **Frontend** (Día 39-42)
8. **Testing & Deploy** (Día 43-50)

---

## ⚙️ DEPENDENCIAS CRÍTICAS

```
Domain (0 dependencias)
  ↑
Application (depende de Domain)
  ↑
Infrastructure (depende de Domain + Application)
  ↑
Api (depende de Application vía IMediator)
```

**Regla de oro:** Nunca importes de capas superiores a inferiores.

---

## 📊 ESTIMACIÓN DE ESFUERZO

| Fase | Días | Horas | % Proyecto |
|------|------|-------|-----------|
| Setup | 3 | 15 | 6% |
| Base de Datos | 6 | 30 | 12% |
| Infrastructure | 3 | 15 | 6% |
| Structure | 3 | 15 | 6% |
| CQRS Features | 20 | 80 | 32% |
| API Controllers | 3 | 15 | 6% |
| Frontend React | 4 | 20 | 8% |
| Testing & Deploy | 8 | 25 | 10% |
| **TOTAL** | **50** | **240** | **100%** |

---

## 💬 CONCLUSIÓN

CGVStockApp implementa **CQRS con MediatR** para:
- ✅ Código limpio y mantenible
- ✅ Fácil testing
- ✅ Escalabilidad futura
- ✅ Auditoría natural
- ✅ Performance optimizable

**Cronograma:** 45-50 días INFLEXIBLE
**Funcionalidades:** 100% según especificación
**Calidad:** Arquitectura enterprise

---

**Próxima acción:** Comenzar FASE 0 (Setup)

Documento de ayuda: `/outputs/01_ARQUITECTURA_CQRS_CGVStockApp.md`
Guía práctica: `/outputs/02_IMPLEMENTACION_PRACTICA_CQRS.md`

# 🏗️ CGVStockApp - ARQUITECTURA REVISADA V2.0
## CQRS + MediatR + EF Core Directo (Sin Repository Pattern)

**Proyecto:** Sistema de Gestión de Ventas y Stock  
**Tecnología:** .NET 9 + ASP.NET Core + PostgreSQL + React  
**Patrón:** CQRS con MediatR (Acceso EF Core Directo)  
**Duración:** 42-47 días (ACTUALIZADO)  
**Estado:** Arquitectura v2.0 Final

---

## 📋 TABLA DE CONTENIDOS

1. [Cambios Principales](#cambios-principales)
2. [Arquitectura General](#arquitectura-general)
3. [Estructura de Proyectos](#estructura-de-proyectos)
4. [Flujo CQRS Detallado](#flujo-cqrs-detallado)
5. [Responsabilidad de Cada Capa](#responsabilidad-de-cada-capa)
6. [IApplicationDbContext](#iapplicationdbcontext)
7. [Patrones de Implementación](#patrones-de-implementación)
8. [Features del Proyecto](#features-del-proyecto)

---

## ⚡ CAMBIOS PRINCIPALES (vs v1.0)

### ❌ ELIMINADO

```
Infrastructure/Repositories/
├── IRepository.cs          ❌ Ya no necesario
├── Repository.cs           ❌ Ya no necesario
├── IProductRepository.cs   ❌ Ya no necesario
├── ProductRepository.cs    ❌ Ya no necesario
├── IUnitOfWork.cs          ❌ Ya no necesario
└── UnitOfWork.cs           ❌ Ya no necesario

Application/DependencyInjection.cs
└── Ya no registra Repositories  ❌
```

### ✅ AGREGADO

```
Application/Common/Interfaces/
└── IApplicationDbContext.cs      ✅ NUEVA

Infrastructure/Persistence/Context/
└── ApplicationDbContext.cs        ✅ Implementa IApplicationDbContext
```

### 📊 COMPARACIÓN DE FLUJO

```
v1.0 (5 capas):
Handler → UnitOfWork → Repository → DbContext → BD

v2.0 (3 capas):
Handler → IApplicationDbContext → BD
```

---

## 🏗️ ARQUITECTURA GENERAL

```
┌─────────────────────────────────────────────────────────┐
│                   React Frontend                        │
│              (PC/Tablet/Mobile)                         │
└────────────┬────────────────────────────────────────────┘
             │ HTTP Request
             ▼
┌─────────────────────────────────────────────────────────┐
│              CGVStockApp.Api (Port 5000)                │
│  ┌──────────────────────────────────────────────────┐   │
│  │ Controllers (Route + Auth + Error Handling)      │   │
│  │  - ProductController                            │   │
│  │  - SalesController                              │   │
│  │  - DashboardController                          │   │
│  │  - UserController                               │   │
│  └─────────────────┬────────────────────────────────┘   │
│                    │                                    │
│  Inyección de MediatR Sender                           │
│  (enruta a Application)                                │
└────────────┬────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────┐
│          CGVStockApp.Application (CQRS LOGIC)          │
│  ┌──────────────────────────────────────────────────┐   │
│  │ COMMANDS (Escriben/Modifican)                    │   │
│  │  ├── CreateProductCommand                       │   │
│  │  ├── UpdateProductCommand                       │   │
│  │  ├── DeleteProductCommand                       │   │
│  │  ├── CreateSaleCommand                          │   │
│  │  └── [Acciones de negocio]                      │   │
│  │     ▼                                             │   │
│  │ HANDLERS (Procesa lógica)                        │   │
│  │  ├── CreateProductCommandHandler                │   │
│  │  │   ├─ Inyecta: IApplicationDbContext           │   │
│  │  │   ├─ Persiste: _context.Products.Add()       │   │
│  │  │   └─ Guarda: _context.SaveChangesAsync()     │   │
│  │  └── Acceso EF Core DIRECTO                     │   │
│  │                                                  │   │
│  │ QUERIES (Leen sin modificar)                    │   │
│  │  ├── GetProductsQuery                           │   │
│  │  ├── GetMinimumStockQuery                       │   │
│  │  └── GetDashboardQuery                          │   │
│  │     ▼                                             │   │
│  │ QUERY HANDLERS (Lee + Proyecta)                │   │
│  │  ├── GetProductsQueryHandler                    │   │
│  │  │   ├─ Inyecta: IApplicationDbContext           │   │
│  │  │   ├─ Lee: _context.Products.Where()          │   │
│  │  │   └─ Proyecta: .Select(p => new DTO)         │   │
│  │  └─ SIN AutoMapper (Select() directo)            │   │
│  │                                                  │   │
│  │ VALIDATORS (FluentValidation)                   │   │
│  │  └─ Ejecutados en MediatR Pipeline               │   │
│  │                                                  │   │
│  │ MAPPINGS (AutoMapper - Solo Complejos)          │   │
│  │  └─ Mapea Commands → Entities (cuando es necesario) │
│  └──────────────────────────────────────────────────┘   │
│                    │                                    │
│  ┌─────────────────▼────────────────────────────────┐   │
│  │ MediatR Pipeline Behaviors                       │   │
│  │  ├── ValidationBehavior (FluentValidation)      │   │
│  │  ├── LoggingBehavior                            │   │
│  │  └── PerformanceBehavior                        │   │
│  └──────────────────────────────────────────────────┘   │
└────────────┬────────────────────────────────────────────┘
             │ Inyecta IApplicationDbContext
             ▼
┌─────────────────────────────────────────────────────────┐
│       CGVStockApp.Infrastructure (ACCESO A DATOS)      │
│  ┌──────────────────────────────────────────────────┐   │
│  │ IApplicationDbContext (Interfaz)                 │   │
│  │  ├── DbSet<Product> Products { get; }           │   │
│  │  ├── DbSet<Category> Categories { get; }        │   │
│  │  ├── DbSet<Sale> Sales { get; }                 │   │
│  │  └── SaveChangesAsync()                         │   │
│  │                                                  │   │
│  │ ApplicationDbContext (Implementación)            │   │
│  │  ├── StockAppDbContext : DbContext,            │   │
│  │  │                       IApplicationDbContext  │   │
│  │  ├── DbSets configurados                        │   │
│  │  └── Migrations                                 │   │
│  │                                                  │   │
│  │ EntityTypeConfigurations (Fluent)               │   │
│  │  ├── ProductConfiguration                       │   │
│  │  ├── CategoryConfiguration                      │   │
│  │  └── ...                                         │   │
│  │                                                  │   │
│  │ Services (Infraestructura)                      │   │
│  │  ├── JwtTokenService                            │   │
│  │  ├── PasswordHashService                        │   │
│  │  └── PdfGenerationService                       │   │
│  └──────────────────────────────────────────────────┘   │
└────────────┬────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────┐
│       CGVStockApp.Domain (ENTIDADES + REGLAS)          │
│  ├── Entities/ (User, Product, Category, Sale, etc)    │
│  ├── Enums/ (RoleType, CustomerType, etc)              │
│  ├── Common/ (AuditableEntity, IAggregateRoot)         │
│  └── Exceptions/ (DomainException, etc)                │
└────────────┬────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────┐
│          PostgreSQL Database                           │
│  ├── products                                          │
│  ├── categories                                        │
│  ├── sales                                             │
│  ├── accounting_movements (Libro Diario)               │
│  └── ... (tablas restantes)                           │
└─────────────────────────────────────────────────────────┘
```

---

## 📁 ESTRUCTURA DE PROYECTOS V2.0

### CGVStockApp.Domain/

```
Domain/
├── Entities/
│   ├── User.cs
│   ├── Role.cs
│   ├── Category.cs
│   ├── Subcategory.cs
│   ├── Product.cs
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

### CGVStockApp.Application/

```
Application/
│
├── Features/
│   ├── Authentication/
│   │   ├── Commands/
│   │   │   └── LoginCommand.cs
│   │   ├── Handlers/
│   │   │   └── LoginCommandHandler.cs
│   │   ├── DTOs/
│   │   │   ├── LoginRequest.cs
│   │   │   └── LoginResponse.cs
│   │   └── Validators/
│   │       └── LoginCommandValidator.cs
│   │
│   ├── Products/
│   │   ├── Commands/
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── UpdateProductCommand.cs
│   │   │   └── DeleteProductCommand.cs
│   │   ├── Handlers/
│   │   │   ├── CreateProductCommandHandler.cs
│   │   │   ├── UpdateProductCommandHandler.cs
│   │   │   └── DeleteProductCommandHandler.cs
│   │   ├── Queries/
│   │   │   ├── GetProductsQuery.cs
│   │   │   ├── GetProductByIdQuery.cs
│   │   │   ├── SearchProductsQuery.cs
│   │   │   └── GetMinimumStockProductsQuery.cs
│   │   ├── QueryHandlers/
│   │   │   ├── GetProductsQueryHandler.cs
│   │   │   ├── GetProductByIdQueryHandler.cs
│   │   │   ├── SearchProductsQueryHandler.cs
│   │   │   └── GetMinimumStockProductsQueryHandler.cs
│   │   ├── DTOs/
│   │   │   ├── CreateProductRequest.cs
│   │   │   ├── UpdateProductRequest.cs
│   │   │   └── ProductResponse.cs
│   │   ├── Validators/
│   │   │   ├── CreateProductCommandValidator.cs
│   │   │   └── UpdateProductCommandValidator.cs
│   │   └── Mappings/
│   │       └── ProductMappingProfile.cs
│   │
│   ├── Categories/
│   │   ├── Commands/
│   │   ├── Handlers/
│   │   ├── Queries/
│   │   ├── QueryHandlers/
│   │   ├── DTOs/
│   │   ├── Validators/
│   │   └── Mappings/
│   │
│   ├── Subcategories/
│   │   └── ... (similar a Categories)
│   │
│   ├── Sales/
│   │   ├── Commands/
│   │   │   └── CreateSaleCommand.cs
│   │   ├── Handlers/
│   │   │   └── CreateSaleCommandHandler.cs
│   │   ├── Queries/
│   │   ├── QueryHandlers/
│   │   ├── DTOs/
│   │   ├── Validators/
│   │   └── Mappings/
│   │
│   ├── Users/
│   │   └── ... (CRUD)
│   │
│   ├── Roles/
│   │   └── ... (CRUD)
│   │
│   └── Dashboard/
│       ├── Queries/
│       │   ├── GetDailySalesQuery.cs
│       │   ├── GetWeeklySalesQuery.cs
│       │   ├── GetMonthlySalesQuery.cs
│       │   └── GetTotalSalesQuery.cs
│       ├── QueryHandlers/
│       ├── DTOs/
│       └── Mappings/ (SOLO AQUÍ si es complejo)
│
├── Common/
│   ├── Interfaces/
│   │   └── IApplicationDbContext.cs     ✅ NUEVO
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs
│   │   ├── LoggingBehavior.cs
│   │   └── PerformanceBehavior.cs
│   ├── Exceptions/
│   │   ├── ApplicationException.cs
│   │   ├── ValidationException.cs
│   │   └── NotFoundException.cs
│   └── Constants/
│       ├── ErrorMessages.cs
│       └── SuccessMessages.cs
│
└── DependencyInjection.cs
```

### CGVStockApp.Infrastructure/

```
Infrastructure/
│
├── Persistence/
│   ├── Context/
│   │   └── ApplicationDbContext.cs      ✅ Implementa IApplicationDbContext
│   ├── Configurations/
│   │   ├── UserConfiguration.cs
│   │   ├── ProductConfiguration.cs
│   │   ├── SaleConfiguration.cs
│   │   └── ... (uno por entidad)
│   └── Migrations/
│       ├── 20240115_InitialCreate.cs
│       └── ... (cronológicas)
│
├── Services/
│   ├── JwtTokenService.cs
│   ├── PasswordHashService.cs
│   └── PdfGenerationService.cs
│
└── DependencyInjection.cs
```

### CGVStockApp.Api/

```
Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── ProductController.cs
│   ├── CategoryController.cs
│   ├── SubcategoryController.cs
│   ├── SalesController.cs
│   ├── UserController.cs
│   └── DashboardController.cs
│
├── Middlewares/
│   └── ErrorHandlingMiddleware.cs
│
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

---

## 🔄 FLUJO CQRS DETALLADO V2.0

### COMANDO: CreateProductCommand

```
1. USUARIO (Frontend)
   └─> POST /api/products
       Body: { name, price, stock, ... }

2. CONTROLLER
   └─> ProductController.CreateAsync(CreateProductRequest)
       ├─ Convierte Request → Command
       └─ Envía: await _mediator.Send(command)

3. MEDIATR PIPELINE
   ├─ ValidationBehavior
   │  └─ Ejecuta CreateProductCommandValidator (FluentValidation)
   │     └─ Valida reglas de negocio
   │
   ├─ LoggingBehavior
   │  └─ Registra: "Iniciando CreateProductCommand"
   │
   └─ PerformanceBehavior
      └─ Mide tiempo de ejecución

4. HANDLER: CreateProductCommandHandler
   ├─ Inyecta: IApplicationDbContext
   ├─ Inyecta: IMapper (solo si es complejo)
   │
   ├─ LÓGICA:
   │  ├─ Validar dependencias (Category existe)
   │  ├─ Generar ProductCode automático
   │  ├─ Crear entidad Product
   │  ├─ Mapear con AutoMapper (CreateProductCommand → Product)
   │  │
   │  ├─ PERSISTENCIA (EF Core Directo):
   │  │  ├─ _context.Products.Add(product)
   │  │  └─ await _context.SaveChangesAsync()
   │  │
   │  └─ Mapear respuesta: Product → ProductResponse
   │     (SIN AutoMapper, manual o Select si es simple)
   │
   └─ RETORNA: ProductResponse

5. MEDIATR
   └─ Retorna resultado al Controller

6. CONTROLLER
   └─ return CreatedAtAction(result)

7. USUARIO
   └─> Recibe 201 Created + ProductResponse
```

### QUERY: GetProductsQuery

```
1. USUARIO (Frontend)
   └─> GET /api/products?categoryId=1

2. CONTROLLER
   └─> ProductController.GetAsync()
       ├─ Convierte parámetros → Query
       └─ Envía: await _mediator.Send(query)

3. MEDIATR PIPELINE
   ├─ ValidationBehavior (si hay validator)
   └─ LoggingBehavior

4. HANDLER: GetProductsQueryHandler
   ├─ Inyecta: IApplicationDbContext
   │
   ├─ LÓGICA (SIN AutoMapper):
   │  ├─ Lee: var query = _context.Products
   │  │          .Where(p => p.IsActive)
   │  │
   │  ├─ PROYECTA (Select() directo):
   │  │  └─ .Select(p => new ProductResponse
   │  │     {
   │  │         Id = p.Id,
   │  │         Name = p.Name,
   │  │         Price = p.PublicPrice,
   │  │         CategoryName = p.Category.Name
   │  │     })
   │  │
   │  └─ EJECUTA: .ToListAsync()
   │
   └─ RETORNA: List<ProductResponse>

5. MEDIATR
   └─ Retorna resultados

6. CONTROLLER
   └─ return Ok(results)

7. USUARIO
   └─> Recibe 200 OK + Array de ProductResponse
```

---

## 📊 RESPONSABILIDAD DE CADA CAPA V2.0

### Domain (Entidades + Reglas)

```csharp
namespace CGVStockApp.Domain.Entities
{
    public class Product : AuditableEntity, IAggregateRoot
    {
        public int Id { get; set; }
        public string ProductCode { get; set; }
        public string Name { get; set; }
        public decimal PublicPrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public int AvailableStock { get; set; }
        public int AlertStock { get; set; }
        
        // LÓGICA DE DOMINIO (No se toca desde Application)
        public void DecreaseStock(int quantity)
        {
            if (quantity > AvailableStock)
                throw new InsufficientStockException(...);
            AvailableStock -= quantity;
        }
        
        public void UpdatePrices(decimal percentageChange)
        {
            decimal factor = 1 + (percentageChange / 100m);
            PublicPrice = Math.Round(PublicPrice * factor, 2);
            WholesalePrice = Math.Round(WholesalePrice * factor, 2);
        }
    }
}
```

✅ **Responsabilidad:** QUÉ es el negocio
❌ **No contiene:** API, Acceso a BD, Controllers

---

### Application (CQRS + Lógica de Casos de Uso)

#### COMMAND + HANDLER (Escriben)

```csharp
// Command (qué hacer)
public class CreateProductCommand : IRequest<ProductResponse>
{
    public string Name { get; set; }
    public decimal PublicPrice { get; set; }
    public int InitialStock { get; set; }
}

// Handler (cómo hacerlo)
public class CreateProductCommandHandler 
    : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly IApplicationDbContext _context;  // ✅ Inyectado
    private readonly IMapper _mapper;                 // ✅ Solo si necesario
    
    public CreateProductCommandHandler(
        IApplicationDbContext context,
        IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    
    public async Task<ProductResponse> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        // Validar dependencias
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);
        
        if (category == null)
            throw new NotFoundException("Categoría no existe");
        
        // Crear entidad
        var product = new Product
        {
            Name = request.Name,
            PublicPrice = request.PublicPrice,
            InitialStock = request.InitialStock,
            AvailableStock = request.InitialStock
        };
        
        // Persistencia: EF Core DIRECTO
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Retornar DTO
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            PublicPrice = product.PublicPrice
        };
    }
}

// Validator
public class CreateProductCommandValidator 
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nombre requerido")
            .MaximumLength(100);
        
        RuleFor(x => x.PublicPrice)
            .GreaterThan(0).WithMessage("Precio debe ser positivo");
    }
}

// Mapping (SOLO si transformación es compleja)
public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<CreateProductCommand, Product>()
            .ForMember(dest => dest.AvailableStock, 
                opt => opt.MapFrom(src => src.InitialStock));
    }
}
```

#### QUERY + HANDLER (Leen)

```csharp
// Query
public class GetProductsQuery : IRequest<List<ProductResponse>>
{
    public int? CategoryId { get; set; }
}

// Handler (SIN AutoMapper - Select() directo)
public class GetProductsQueryHandler 
    : IRequestHandler<GetProductsQuery, List<ProductResponse>>
{
    private readonly IApplicationDbContext _context;  // ✅ Inyectado
    
    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<ProductResponse>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        // Lectura + Proyección (SIN AutoMapper)
        return await _context.Products
            .Where(p => p.IsActive)
            .Where(p => !request.CategoryId.HasValue || p.CategoryId == request.CategoryId)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                Name = p.Name,
                PublicPrice = p.PublicPrice,
                WholesalePrice = p.WholesalePrice,
                AvailableStock = p.AvailableStock,
                CategoryName = p.Category.Name
            })
            .OrderBy(p => p.CategoryName)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
```

✅ **Responsabilidad:** CÓMO ejecutar casos de uso (CQRS)
❌ **No contiene:** Controllers, Acceso directo a BD (va via IApplicationDbContext)

---

### Infrastructure (Persistencia + Servicios Técnicos)

```csharp
// IApplicationDbContext (interfaz)
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<Sale> Sales { get; }
    DbSet<User> Users { get; }
    DbSet<AccountingMovement> AccountingMovements { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// ApplicationDbContext (implementación)
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
    
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AccountingMovement> AccountingMovements { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Aplicar configuraciones
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new SaleConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}

// Configuración Fluent
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.ProductCode)
            .IsRequired()
            .HasMaxLength(10);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);
        
        builder.Property(p => p.PublicPrice)
            .HasPrecision(10, 2);
        
        // Índices y constraints
        builder.HasIndex(p => p.ProductCode).IsUnique();
    }
}

// Servicios de Infraestructura
public class JwtTokenService
{
    public string GenerateToken(User user, string secretKey, int expirationMinutes)
    {
        // Implementación JWT...
    }
}

public class PasswordHashService
{
    public string HashPassword(string password) { ... }
    public bool VerifyPassword(string password, string hash) { ... }
}

public class PdfGenerationService
{
    public byte[] GeneratePdf(List<Product> products) { ... }
}
```

✅ **Responsabilidad:** Detalles técnicos (BD, servicios externos)
❌ **No contiene:** Lógica de negocio, Controllers

---

### Api (Controllers HTTP)

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IMediator _mediator;  // ✅ Inyectado
    
    public ProductController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost("create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request)
    {
        var command = new CreateProductCommand
        {
            Name = request.Name,
            PublicPrice = request.PublicPrice,
            InitialStock = request.InitialStock
        };
        
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int? categoryId)
    {
        var query = new GetProductsQuery { CategoryId = categoryId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

✅ **Responsabilidad:** Exponer endpoints HTTP
❌ **No contiene:** Lógica de negocio

---

## 🔌 IApplicationDbContext

### Definición

```csharp
// Application/Common/Interfaces/IApplicationDbContext.cs
namespace CGVStockApp.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<Category> Categories { get; }
        DbSet<Subcategory> Subcategories { get; }
        DbSet<Product> Products { get; }
        DbSet<Sale> Sales { get; }
        DbSet<SaleDetail> SaleDetails { get; }
        DbSet<StockMovement> StockMovements { get; }
        DbSet<AccountingMovement> AccountingMovements { get; }
        
        /// <summary>
        /// Persiste los cambios realizados en el contexto a la base de datos.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
```

### Implementación

```csharp
// Infrastructure/Persistence/Context/ApplicationDbContext.cs
namespace CGVStockApp.Infrastructure.Persistence.Context
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleDetail> SaleDetails { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<AccountingMovement> AccountingMovements { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            modelBuilder.ApplyConfiguration(new SubcategoryConfiguration());
            modelBuilder.ApplyConfiguration(new SaleConfiguration());
            modelBuilder.ApplyConfiguration(new SaleDetailConfiguration());
            modelBuilder.ApplyConfiguration(new AccountingMovementConfiguration());
        }
    }
}
```

### Inyección en Program.cs

```csharp
// Api/Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar interfaz
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

// Resto de servicios...
builder.Services.AddMediatR(typeof(Application.DependencyInjection).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Application.DependencyInjection).Assembly);
builder.Services.AddAutoMapper(typeof(Application.DependencyInjection).Assembly);

var app = builder.Build();
// ...
```

---

## 🎯 PATRONES DE IMPLEMENTACIÓN V2.0

### 1. CQRS + MediatR (Sin Repository Pattern)

```
COMMAND FLOW:
Request → Controller → Command → Handler → DbContext → BD

QUERY FLOW:
Request → Controller → Query → Handler → DbContext (Select()) → DTO → Response
```

### 2. Validación con FluentValidation

```csharp
// En MediatR Pipeline (automático)
ValidationBehavior<TRequest, TResponse>
    ├─ Obtiene todos los IValidator<TRequest>
    ├─ Ejecuta validaciones
    └─ Lanza ValidationException si hay errores
```

### 3. AutoMapper Solo Cuando es Necesario

```
CASO 1: Command complejo → Entity (SÍ usar AutoMapper)
CreateProductCommand → Product
(porque tiene transformaciones)

CASO 2: Entity → DTO (NO usar AutoMapper)
Product → ProductResponse
(usar Select() directo en Query)

CASO 3: DTO → Command (NO necesario)
Convertir manualmente en Controller
```

### 4. Proyecciones con Select()

```csharp
// En Queries
var result = await _context.Products
    .Select(p => new ProductResponse
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.PublicPrice,
        CategoryName = p.Category.Name
    })
    .ToListAsync();  // SELECT ejecutado en BD
```

### 5. Manejo de Excepciones

```csharp
// Domain Exceptions (lanzadas en lógica de dominio)
if (quantity > AvailableStock)
    throw new InsufficientStockException("...");

// Application Exceptions (lanzadas en handlers)
var category = await _context.Categories.FirstOrDefaultAsync(...);
if (category == null)
    throw new NotFoundException("La categoría no existe");

// Middleware maneja ambas
public class ErrorHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex) { return 400; }
        catch (NotFoundException ex) { return 404; }
        catch (DomainException ex) { return 400; }
        catch (Exception ex) { return 500; }
    }
}
```

---

## 🎪 FEATURES DEL PROYECTO V2.0

### 1. Authentication
- LoginCommand + Handler
- JWT Token Generation
- Custom User + JWT (sin Identity)

### 2. Products
- CreateProductCommand (genera ProductCode automático)
- UpdateProductCommand
- DeleteProductCommand
- GetProductsQuery
- SearchProductsQuery
- GetMinimumStockProductsQuery (para PDF)

### 3. Categories
- CRUD Commands
- Get Queries

### 4. Subcategories
- CRUD Commands
- Get Queries

### 5. Sales
- CreateSaleCommand (valida stock, actualiza, registra en AccountingMovement)
- GetSalesQuery
- GetSalesByPeriodQuery

### 6. Users
- CRUD Commands

### 7. Roles
- CRUD Commands

### 8. Dashboard
- GetDailySalesQuery
- GetWeeklySalesQuery
- GetMonthlySalesQuery
- GetTotalSalesQuery
- Lee desde AccountingMovements

---

## 🔗 REGLAS DE DEPENDENCIAS (Clean Architecture)

```
Domain     ← NO depende de nada
   ↑
   │ depende de
   │
Application  ← Depende de Domain
   ↑
   │ depende de
   │
Infrastructure ← Depende de Domain + Application
   ↑
   │ depende de
   │
Api        ← Depende de Application
```

**NUNCA:**
- ❌ Infrastructure → Application (invertir flujo)
- ❌ Domain → Application
- ❌ Domain → Infrastructure
- ❌ Api → Infrastructure (directo)

---

## 📊 IMPACTO EN CRONOGRAMA

| Cambio | Días Ahorrados |
|--------|---|
| Eliminar Repository Pattern | -2 días |
| AutoMapper limitado | -1 día |
| Sin Identity (Custom + JWT) | 0 días |
| **TOTAL** | **-3 días** ✅ |

**Nuevo cronograma:** 42-47 días (vs 45-50 días original)

---

## ✅ CONCLUSIÓN

CGVStockApp v2.0:
- ✅ **Más limpia:** Sin Repository Pattern innecesario
- ✅ **Más rápida:** AutoMapper solo donde necesario
- ✅ **Más clara:** IApplicationDbContext explícito
- ✅ **Más mantenible:** CQRS con MediatR
- ✅ **Funcional:** 100% de requisitos
- ✅ **Eficiente:** -3 días en cronograma

**Arquitectura final:** CQRS + MediatR + EF Core Directo + Clean Architecture

---

**Próximos documentos:**
1. ✏️ Implementación Práctica v2.0 (código listo)
2. ✏️ Roadmap v2.0 (días 1-47)

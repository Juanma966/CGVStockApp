# 💻 GUÍA DE IMPLEMENTACIÓN CQRS + MediatR
## CGVStockApp - Ejemplos Prácticos Paso a Paso

---

## 📋 ÍNDICE
1. [Setup Inicial](#setup-inicial)
2. [Crear una Feature Completa](#crear-feature-completa)
3. [Ejemplo: Product Feature](#ejemplo-product-feature)
4. [Ejemplo: Sale Feature](#ejemplo-sale-feature)
5. [Ejemplo: Dashboard Feature](#ejemplo-dashboard-feature)
6. [Configuración Global](#configuración-global)
7. [Testing](#testing)

---

## 🚀 SETUP INICIAL (DÍAS 1-3)

### Paso 1: Crear Solución

```powershell
# Carpeta de trabajo
mkdir C:\Users\YourUser\Desktop\CGVStockApp
cd C:\Users\YourUser\Desktop\CGVStockApp

# Crear solución
dotnet new sln -n CGVStockApp

# Crear proyectos
dotnet new classlib -n CGVStockApp.Domain
dotnet new classlib -n CGVStockApp.Application
dotnet new classlib -n CGVStockApp.Infrastructure
dotnet new webapi -n CGVStockApp.Api

# Agregar a solución
dotnet sln add CGVStockApp.Domain/CGVStockApp.Domain.csproj
dotnet sln add CGVStockApp.Application/CGVStockApp.Application.csproj
dotnet sln add CGVStockApp.Infrastructure/CGVStockApp.Infrastructure.csproj
dotnet sln add CGVStockApp.Api/CGVStockApp.Api.csproj
```

### Paso 2: Instalar Dependencias

```powershell
# En Domain (solo Microsoft.Extensions para IEntity)
cd CGVStockApp.Domain
dotnet add package Microsoft.EntityFrameworkCore.Abstractions
cd ..

# En Application
cd CGVStockApp.Application
dotnet add package MediatR
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
cd ..

# En Infrastructure
cd CGVStockApp.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package itext7
cd ..

# En Api
cd CGVStockApp.Api
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
cd ..

# Verificar build
dotnet build
```

### Paso 3: Agregar referencias entre proyectos

```powershell
# Application depende de Domain
cd CGVStockApp.Application
dotnet add reference ../CGVStockApp.Domain/CGVStockApp.Domain.csproj
cd ..

# Infrastructure depende de Domain y Application
cd CGVStockApp.Infrastructure
dotnet add reference ../CGVStockApp.Domain/CGVStockApp.Domain.csproj
dotnet add reference ../CGVStockApp.Application/CGVStockApp.Application.csproj
cd ..

# Api depende de Application
cd CGVStockApp.Api
dotnet add reference ../CGVStockApp.Application/CGVStockApp.Application.csproj
cd ..

dotnet build
```

---

## 📦 CREAR UNA FEATURE COMPLETA

### Anatomía de una Feature CQRS

```
Features/
├── YourFeature/
│   ├── Commands/
│   │   └── CreateYourEntityCommand.cs          # El "qué hacer"
│   ├── Handlers/
│   │   └── CreateYourEntityCommandHandler.cs   # El "cómo hacerlo"
│   ├── Queries/
│   │   └── GetYourEntitiesQuery.cs             # El "qué traer"
│   ├── QueryHandlers/
│   │   └── GetYourEntitiesQueryHandler.cs      # El "cómo traerlo"
│   ├── DTOs/
│   │   ├── CreateYourEntityRequest.cs          # Input
│   │   └── YourEntityResponse.cs               # Output
│   ├── Validators/
│   │   └── CreateYourEntityValidator.cs        # Validaciones
│   └── Mappings/
│       └── YourEntityMappingProfile.cs         # Auto Mapper
```

---

## 🎯 EJEMPLO: PRODUCT FEATURE (COMPLETO)

### 1. Domain/Entities/Product.cs

```csharp
using CGVStockApp.Domain.Common;

namespace CGVStockApp.Domain.Entities
{
    public class Product : AuditableEntity, IAggregateRoot
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } // Formato AANNNN
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal PublicPrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public int InitialStock { get; set; }
        public int AvailableStock { get; set; }
        public int AlertStock { get; set; }
        
        // Foreign Keys
        public int? CategoryId { get; set; }
        public Category Category { get; set; }
        
        public int? SubcategoryId { get; set; }
        public Subcategory Subcategory { get; set; }
        
        // Navigation
        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        
        public bool IsActive { get; set; } = true;
        
        // Lógica de dominio
        public void DecreaseStock(int quantity)
        {
            if (quantity > AvailableStock)
                throw new InvalidOperationException(
                    $"Stock insuficiente. Disponible: {AvailableStock}, solicitado: {quantity}");
            
            AvailableStock -= quantity;
        }
        
        public void UpdatePrices(decimal pricePercentageChange)
        {
            decimal factor = 1 + (pricePercentageChange / 100m);
            PublicPrice = Math.Round(PublicPrice * factor, 2);
            WholesalePrice = Math.Round(WholesalePrice * factor, 2);
        }
    }
}
```

### 2. Domain/Common/AuditableEntity.cs

```csharp
namespace CGVStockApp.Domain.Common
{
    public abstract class AuditableEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string ModifiedBy { get; set; }
    }
}
```

### 3. Domain/Common/IAggregateRoot.cs

```csharp
namespace CGVStockApp.Domain.Common
{
    public interface IAggregateRoot { }
}
```

### 4. Infrastructure/Persistence/Context/StockAppDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Domain.Entities;
using CGVStockApp.Infrastructure.Persistence.Configurations;

namespace CGVStockApp.Infrastructure.Persistence.Context
{
    public class StockAppDbContext : DbContext
    {
        public StockAppDbContext(DbContextOptions<StockAppDbContext> options)
            : base(options) { }
        
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleDetail> SaleDetails { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Aplicar todas las configuraciones
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            modelBuilder.ApplyConfiguration(new SubcategoryConfiguration());
            modelBuilder.ApplyConfiguration(new SaleConfiguration());
            modelBuilder.ApplyConfiguration(new SaleDetailConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
        }
    }
}
```

### 5. Infrastructure/Persistence/Configurations/ProductConfiguration.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CGVStockApp.Domain.Entities;

namespace CGVStockApp.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("products");
            builder.HasKey(p => p.Id);
            
            // Propiedades
            builder.Property(p => p.ProductCode)
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnName("product_code");
            
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnName("name");
            
            builder.Property(p => p.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            
            builder.Property(p => p.PublicPrice)
                .HasPrecision(10, 2)
                .HasColumnName("public_price");
            
            builder.Property(p => p.WholesalePrice)
                .HasPrecision(10, 2)
                .HasColumnName("wholesale_price");
            
            builder.Property(p => p.InitialStock)
                .HasColumnName("initial_stock");
            
            builder.Property(p => p.AvailableStock)
                .HasColumnName("available_stock");
            
            builder.Property(p => p.AlertStock)
                .HasColumnName("alert_stock");
            
            builder.Property(p => p.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            
            // Auditoría
            builder.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            builder.Property(p => p.CreatedBy)
                .HasColumnName("created_by");
            
            builder.Property(p => p.ModifiedAt)
                .HasColumnName("modified_at");
            
            builder.Property(p => p.ModifiedBy)
                .HasColumnName("modified_by");
            
            // Relaciones
            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            
            builder.HasOne(p => p.Subcategory)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SubcategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Índices
            builder.HasIndex(p => p.ProductCode).IsUnique();
            builder.HasIndex(p => p.Name);
            builder.HasIndex(p => p.CategoryId);
        }
    }
}
```

### 6. Application/Features/Products/DTOs/CreateProductRequest.cs

```csharp
namespace CGVStockApp.Application.Features.Products.DTOs
{
    public class CreateProductRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal PublicPrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public int InitialStock { get; set; }
        public int AlertStock { get; set; }
        public int? CategoryId { get; set; }
        public int? SubcategoryId { get; set; }
    }
}
```

### 7. Application/Features/Products/DTOs/ProductResponse.cs

```csharp
namespace CGVStockApp.Application.Features.Products.DTOs
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string ProductCode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal PublicPrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public int InitialStock { get; set; }
        public int AvailableStock { get; set; }
        public int AlertStock { get; set; }
        public string CategoryName { get; set; }
        public string SubcategoryName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
```

### 8. Application/Features/Products/Commands/CreateProductCommand.cs

```csharp
using MediatR;
using CGVStockApp.Application.Features.Products.DTOs;

namespace CGVStockApp.Application.Features.Products.Commands
{
    public class CreateProductCommand : IRequest<ProductResponse>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal PublicPrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public int InitialStock { get; set; }
        public int AlertStock { get; set; }
        public int? CategoryId { get; set; }
        public int? SubcategoryId { get; set; }
    }
}
```

### 9. Application/Features/Products/Validators/CreateProductCommandValidator.cs

```csharp
using FluentValidation;
using CGVStockApp.Application.Features.Products.Commands;

namespace CGVStockApp.Application.Features.Products.Validators
{
    public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del producto es requerido")
                .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres");
            
            RuleFor(x => x.PublicPrice)
                .GreaterThan(0).WithMessage("El precio público debe ser mayor a 0");
            
            RuleFor(x => x.WholesalePrice)
                .GreaterThan(0).WithMessage("El precio mayorista debe ser mayor a 0");
            
            RuleFor(x => x.InitialStock)
                .GreaterThanOrEqualTo(0).WithMessage("El stock inicial no puede ser negativo");
            
            RuleFor(x => x.AlertStock)
                .GreaterThanOrEqualTo(0).WithMessage("El stock de alerta no puede ser negativo")
                .LessThanOrEqualTo(x => x.InitialStock)
                .WithMessage("El stock de alerta no puede ser mayor que el stock inicial");
        }
    }
}
```

### 10. Application/Features/Products/Handlers/CreateProductCommandHandler.cs

```csharp
using MediatR;
using AutoMapper;
using CGVStockApp.Application.Features.Products.Commands;
using CGVStockApp.Application.Features.Products.DTOs;
using CGVStockApp.Domain.Entities;
using CGVStockApp.Infrastructure.Persistence.Context;

namespace CGVStockApp.Application.Features.Products.Handlers
{
    public class CreateProductCommandHandler 
        : IRequestHandler<CreateProductCommand, ProductResponse>
    {
        private readonly StockAppDbContext _context;
        private readonly IMapper _mapper;
        
        public CreateProductCommandHandler(
            StockAppDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        
        public async Task<ProductResponse> Handle(
            CreateProductCommand request,
            CancellationToken cancellationToken)
        {
            // Validar que la categoría existe (si se proporciona)
            if (request.CategoryId.HasValue)
            {
                var category = await _context.Categories
                    .FindAsync(new object[] { request.CategoryId.Value }, cancellationToken: cancellationToken);
                
                if (category == null)
                    throw new KeyNotFoundException("La categoría no existe");
            }
            
            // Validar que la subcategoría existe (si se proporciona)
            if (request.SubcategoryId.HasValue)
            {
                var subcategory = await _context.Subcategories
                    .FindAsync(new object[] { request.SubcategoryId.Value }, cancellationToken: cancellationToken);
                
                if (subcategory == null)
                    throw new KeyNotFoundException("La subcategoría no existe");
            }
            
            // Generar ProductCode (AANNNN)
            string categoryLetter = "P"; // Por defecto
            string subcategoryLetter = "0";
            
            if (request.CategoryId.HasValue)
            {
                var category = await _context.Categories
                    .FindAsync(new object[] { request.CategoryId.Value }, cancellationToken: cancellationToken);
                categoryLetter = category?.Name?.FirstOrDefault().ToString().ToUpper() ?? "P";
            }
            
            if (request.SubcategoryId.HasValue)
            {
                var subcategory = await _context.Subcategories
                    .FindAsync(new object[] { request.SubcategoryId.Value }, cancellationToken: cancellationToken);
                subcategoryLetter = subcategory?.Name?.FirstOrDefault().ToString().ToUpper() ?? "0";
            }
            
            // Obtener próximo número
            var lastProduct = _context.Products
                .Where(p => p.ProductCode.StartsWith(categoryLetter + subcategoryLetter))
                .OrderByDescending(p => p.ProductCode)
                .FirstOrDefault();
            
            int nextNumber = 1;
            if (lastProduct != null)
            {
                var lastNumber = int.Parse(lastProduct.ProductCode.Substring(2));
                nextNumber = lastNumber + 1;
            }
            
            string productCode = $"{categoryLetter}{subcategoryLetter}{nextNumber:D4}";
            
            // Crear entidad
            var product = new Product
            {
                ProductCode = productCode,
                Name = request.Name,
                Description = request.Description,
                PublicPrice = request.PublicPrice,
                WholesalePrice = request.WholesalePrice,
                InitialStock = request.InitialStock,
                AvailableStock = request.InitialStock,
                AlertStock = request.AlertStock,
                CategoryId = request.CategoryId,
                SubcategoryId = request.SubcategoryId,
                IsActive = true
            };
            
            // Persistir
            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);
            
            // Mapear y retornar
            return _mapper.Map<ProductResponse>(product);
        }
    }
}
```

### 11. Application/Features/Products/Queries/GetProductsQuery.cs

```csharp
using MediatR;
using CGVStockApp.Application.Features.Products.DTOs;

namespace CGVStockApp.Application.Features.Products.Queries
{
    public class GetProductsQuery : IRequest<List<ProductResponse>>
    {
        public int? CategoryId { get; set; }
        public int? SubcategoryId { get; set; }
        public bool OnlyActive { get; set; } = true;
    }
}
```

### 12. Application/Features/Products/QueryHandlers/GetProductsQueryHandler.cs

```csharp
using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Application.Features.Products.Queries;
using CGVStockApp.Application.Features.Products.DTOs;
using CGVStockApp.Infrastructure.Persistence.Context;

namespace CGVStockApp.Application.Features.Products.QueryHandlers
{
    public class GetProductsQueryHandler 
        : IRequestHandler<GetProductsQuery, List<ProductResponse>>
    {
        private readonly StockAppDbContext _context;
        private readonly IMapper _mapper;
        
        public GetProductsQueryHandler(
            StockAppDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        
        public async Task<List<ProductResponse>> Handle(
            GetProductsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Subcategory)
                .AsQueryable();
            
            if (request.OnlyActive)
                query = query.Where(p => p.IsActive);
            
            if (request.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == request.CategoryId);
            
            if (request.SubcategoryId.HasValue)
                query = query.Where(p => p.SubcategoryId == request.SubcategoryId);
            
            var products = await query
                .OrderBy(p => p.Category.Name)
                .ThenBy(p => p.Subcategory.Name)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);
            
            return _mapper.Map<List<ProductResponse>>(products);
        }
    }
}
```

### 13. Application/Features/Products/Queries/GetMinimumStockProductsQuery.cs

```csharp
using MediatR;
using CGVStockApp.Application.Features.Products.DTOs;

namespace CGVStockApp.Application.Features.Products.Queries
{
    public class GetMinimumStockProductsQuery : IRequest<List<ProductResponse>>
    {
    }
}
```

### 14. Application/Features/Products/QueryHandlers/GetMinimumStockProductsQueryHandler.cs

```csharp
using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Application.Features.Products.Queries;
using CGVStockApp.Application.Features.Products.DTOs;
using CGVStockApp.Infrastructure.Persistence.Context;

namespace CGVStockApp.Application.Features.Products.QueryHandlers
{
    public class GetMinimumStockProductsQueryHandler 
        : IRequestHandler<GetMinimumStockProductsQuery, List<ProductResponse>>
    {
        private readonly StockAppDbContext _context;
        private readonly IMapper _mapper;
        
        public GetMinimumStockProductsQueryHandler(
            StockAppDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        
        public async Task<List<ProductResponse>> Handle(
            GetMinimumStockProductsQuery request,
            CancellationToken cancellationToken)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Subcategory)
                .Where(p => p.AvailableStock <= p.AlertStock && p.IsActive)
                .OrderBy(p => p.Category.Name)
                .ThenBy(p => p.Subcategory.Name)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);
            
            return _mapper.Map<List<ProductResponse>>(products);
        }
    }
}
```

### 15. Application/Features/Products/Mappings/ProductMappingProfile.cs

```csharp
using AutoMapper;
using CGVStockApp.Application.Features.Products.Commands;
using CGVStockApp.Application.Features.Products.DTOs;
using CGVStockApp.Domain.Entities;

namespace CGVStockApp.Application.Features.Products.Mappings
{
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            CreateMap<CreateProductCommand, Product>();
            
            CreateMap<CreateProductRequest, CreateProductCommand>();
            
            CreateMap<Product, ProductResponse>()
                .ForMember(dest => dest.CategoryName, 
                    opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.SubcategoryName, 
                    opt => opt.MapFrom(src => src.Subcategory.Name));
        }
    }
}
```

### 16. Api/Controllers/ProductController.cs

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CGVStockApp.Application.Features.Products.Commands;
using CGVStockApp.Application.Features.Products.Queries;
using CGVStockApp.Application.Features.Products.DTOs;

namespace CGVStockApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IMediator _mediator;
        
        public ProductController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            var command = new CreateProductCommand
            {
                Name = request.Name,
                Description = request.Description,
                PublicPrice = request.PublicPrice,
                WholesalePrice = request.WholesalePrice,
                InitialStock = request.InitialStock,
                AlertStock = request.AlertStock,
                CategoryId = request.CategoryId,
                SubcategoryId = request.SubcategoryId
            };
            
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, result);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int? categoryId,
            [FromQuery] int? subcategoryId)
        {
            var query = new GetProductsQuery
            {
                CategoryId = categoryId,
                SubcategoryId = subcategoryId
            };
            
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            // Implementar GetProductByIdQuery
            return Ok();
        }
        
        [HttpGet("minimum-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetMinimumStockProducts()
        {
            var query = new GetMinimumStockProductsQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
```

---

## 💼 EJEMPLO: SALE FEATURE (MÁS COMPLEJA)

### 1. Domain/Entities/Sale.cs

```csharp
using CGVStockApp.Domain.Common;
using CGVStockApp.Domain.Enums;

namespace CGVStockApp.Domain.Entities
{
    public class Sale : AuditableEntity, IAggregateRoot
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        
        public CustomerType CustomerType { get; set; } // Final o Mayorista
        public PaymentMethodType PaymentMethod { get; set; } // Efectivo, Tarjeta, Transferencia
        public decimal Total { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        
        public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
        
        // Lógica de dominio
        public void CalculateTotal()
        {
            Total = Details.Sum(d => d.Subtotal);
        }
        
        public decimal GetAppliedPrice(Product product)
        {
            return CustomerType == CustomerType.Wholesale 
                ? product.WholesalePrice 
                : product.PublicPrice;
        }
    }
}
```

### 2. Domain/Enums/CustomerType.cs

```csharp
namespace CGVStockApp.Domain.Enums
{
    public enum CustomerType
    {
        Individual = 1,
        Wholesale = 2
    }
}
```

### 3. Application/Features/Sales/Commands/CreateSaleCommand.cs

```csharp
using MediatR;
using CGVStockApp.Application.Features.Sales.DTOs;
using CGVStockApp.Domain.Enums;

namespace CGVStockApp.Application.Features.Sales.Commands
{
    public class CreateSaleCommand : IRequest<SaleResponse>
    {
        public int UserId { get; set; }
        public CustomerType CustomerType { get; set; }
        public PaymentMethodType PaymentMethod { get; set; }
        public List<SaleDetailRequest> Details { get; set; } = new();
    }
    
    public class SaleDetailRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
```

### 4. Application/Features/Sales/Validators/CreateSaleCommandValidator.cs

```csharp
using FluentValidation;
using CGVStockApp.Application.Features.Sales.Commands;

namespace CGVStockApp.Application.Features.Sales.Validators
{
    public class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
    {
        public CreateSaleCommandValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Usuario requerido");
            
            RuleFor(x => x.Details)
                .NotEmpty().WithMessage("Debe agregar al menos un producto");
            
            RuleForEach(x => x.Details).SetValidator(new SaleDetailValidator());
        }
    }
    
    public class SaleDetailValidator : AbstractValidator<SaleDetailRequest>
    {
        public SaleDetailValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("Producto requerido");
            
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Cantidad debe ser mayor a 0");
        }
    }
}
```

### 5. Application/Features/Sales/Handlers/CreateSaleCommandHandler.cs

```csharp
using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Application.Features.Sales.Commands;
using CGVStockApp.Application.Features.Sales.DTOs;
using CGVStockApp.Domain.Entities;
using CGVStockApp.Domain.Enums;
using CGVStockApp.Infrastructure.Persistence.Context;

namespace CGVStockApp.Application.Features.Sales.Handlers
{
    public class CreateSaleCommandHandler 
        : IRequestHandler<CreateSaleCommand, SaleResponse>
    {
        private readonly StockAppDbContext _context;
        private readonly IMapper _mapper;
        
        public CreateSaleCommandHandler(
            StockAppDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        
        public async Task<SaleResponse> Handle(
            CreateSaleCommand request,
            CancellationToken cancellationToken)
        {
            // Validar usuario existe
            var user = await _context.Users
                .FindAsync(new object[] { request.UserId }, cancellationToken: cancellationToken);
            
            if (user == null)
                throw new KeyNotFoundException("El usuario no existe");
            
            // Crear venta
            var sale = new Sale
            {
                UserId = request.UserId,
                CustomerType = request.CustomerType,
                PaymentMethod = request.PaymentMethod
            };
            
            // Procesar detalles y validar stock
            foreach (var detail in request.Details)
            {
                var product = await _context.Products
                    .FindAsync(new object[] { detail.ProductId }, cancellationToken: cancellationToken);
                
                if (product == null)
                    throw new KeyNotFoundException($"Producto {detail.ProductId} no existe");
                
                // Validar stock
                if (product.AvailableStock < detail.Quantity)
                    throw new InvalidOperationException(
                        $"Stock insuficiente para {product.Name}. " +
                        $"Disponible: {product.AvailableStock}, solicitado: {detail.Quantity}");
                
                // Crear detalle
                var appliedPrice = sale.GetAppliedPrice(product);
                
                var saleDetail = new SaleDetail
                {
                    ProductId = product.Id,
                    Quantity = detail.Quantity,
                    UnitPrice = appliedPrice,
                    Subtotal = appliedPrice * detail.Quantity
                };
                
                sale.Details.Add(saleDetail);
                
                // Descontar stock (lógica de dominio)
                product.DecreaseStock(detail.Quantity);
                
                // Registrar movimiento de stock
                var movement = new StockMovement
                {
                    ProductId = product.Id,
                    Type = StockMovementType.Sale,
                    Quantity = -detail.Quantity,
                    Reason = $"Venta #{sale.Id}"
                };
                
                _context.StockMovements.Add(movement);
            }
            
            // Calcular total
            sale.CalculateTotal();
            
            // Registrar en libro diario (Contabilidad)
            var accountMovement = new AccountingMovement
            {
                MovementDate = sale.SaleDate,
                Type = AccountingMovementType.Sale,
                Description = $"Venta #{sale.Id}",
                Amount = sale.Total,
                IsIncome = true,
                SaleId = sale.Id,
                UserId = request.UserId
            };
            
            _context.Sales.Add(sale);
            _context.AccountingMovements.Add(accountMovement);
            await _context.SaveChangesAsync(cancellationToken);
            
            return _mapper.Map<SaleResponse>(sale);
        }
    }
}
```

---

## 📊 EJEMPLO: DASHBOARD FEATURE (QUERIES COMPLEJAS)

### 1. Application/Features/Dashboard/Queries/GetDailyDashboardQuery.cs

```csharp
using MediatR;
using CGVStockApp.Application.Features.Dashboard.DTOs;

namespace CGVStockApp.Application.Features.Dashboard.Queries
{
    public class GetDailyDashboardQuery : IRequest<DashboardResponse>
    {
        public DateTime Date { get; set; } = DateTime.Today;
    }
}
```

### 2. Application/Features/Dashboard/Handlers/GetDailyDashboardQueryHandler.cs

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Application.Features.Dashboard.Queries;
using CGVStockApp.Application.Features.Dashboard.DTOs;
using CGVStockApp.Infrastructure.Persistence.Context;

namespace CGVStockApp.Application.Features.Dashboard.QueryHandlers
{
    public class GetDailyDashboardQueryHandler 
        : IRequestHandler<GetDailyDashboardQuery, DashboardResponse>
    {
        private readonly StockAppDbContext _context;
        
        public GetDailyDashboardQueryHandler(StockAppDbContext context)
        {
            _context = context;
        }
        
        public async Task<DashboardResponse> Handle(
            GetDailyDashboardQuery request,
            CancellationToken cancellationToken)
        {
            var startOfDay = request.Date.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);
            
            // Obtener movimientos del día
            var movements = await _context.AccountingMovements
                .Where(m => m.MovementDate >= startOfDay && m.MovementDate <= endOfDay)
                .ToListAsync(cancellationToken);
            
            var incomes = movements
                .Where(m => m.IsIncome)
                .Sum(m => m.Amount);
            
            var expenses = movements
                .Where(m => !m.IsIncome)
                .Sum(m => m.Amount);
            
            var balance = incomes - expenses;
            
            return new DashboardResponse
            {
                Period = "Diario",
                Date = request.Date,
                TotalIncomes = incomes,
                TotalExpenses = expenses,
                Balance = balance,
                Movements = movements
                    .Select(m => new MovementDto
                    {
                        Date = m.MovementDate,
                        Type = m.Type.ToString(),
                        Description = m.Description,
                        Amount = m.Amount,
                        IsIncome = m.IsIncome
                    })
                    .ToList()
            };
        }
    }
}
```

### 3. Application/Features/Dashboard/DTOs/DashboardResponse.cs

```csharp
namespace CGVStockApp.Application.Features.Dashboard.DTOs
{
    public class DashboardResponse
    {
        public string Period { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalIncomes { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal Balance { get; set; }
        public List<MovementDto> Movements { get; set; }
    }
    
    public class MovementDto
    {
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public bool IsIncome { get; set; }
    }
}
```

---

## ⚙️ CONFIGURACIÓN GLOBAL

### 1. Application/DependencyInjection.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using FluentValidation;
using AutoMapper;
using CGVStockApp.Application.Common.Behaviors;
using CGVStockApp.Application.Features.Products.Mappings;

namespace CGVStockApp.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // MediatR
            services.AddMediatR(typeof(DependencyInjection).Assembly);
            
            // Pipeline Behaviors
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            
            // FluentValidation
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
            
            // AutoMapper
            services.AddAutoMapper(typeof(ProductMappingProfile).Assembly);
            
            return services;
        }
    }
}
```

### 2. Infrastructure/DependencyInjection.cs

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Infrastructure.Persistence.Context;
using CGVStockApp.Infrastructure.Repositories;

namespace CGVStockApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // DbContext
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<StockAppDbContext>(options =>
                options.UseNpgsql(connectionString));
            
            // Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ISalesRepository, SalesRepository>();
            
            // Services
            services.AddScoped<JwtTokenService>();
            services.AddScoped<PasswordHashService>();
            services.AddScoped<PdfGenerationService>();
            
            return services;
        }
    }
}
```

### 3. Api/Program.cs

```csharp
using CGVStockApp.Application;
using CGVStockApp.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = "CGVStockApp",
            ValidateAudience = true,
            ValidAudience = "CGVStockAppClient",
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 4. Application/Common/Behaviors/ValidationBehavior.cs

```csharp
using MediatR;
using FluentValidation;

namespace CGVStockApp.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> 
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }
        
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();
            
            var context = new ValidationContext<TRequest>(request);
            var failures = new List<FluentValidation.Results.ValidationFailure>();
            
            foreach (var validator in _validators)
            {
                var result = await validator.ValidateAsync(context, cancellationToken);
                failures.AddRange(result.Errors);
            }
            
            if (failures.Any())
                throw new ValidationException("Validation failed", failures);
            
            return await next();
        }
    }
}
```

---

## 🧪 TESTING

### Unit Test Ejemplo

```csharp
using Xunit;
using Moq;
using AutoMapper;
using CGVStockApp.Application.Features.Products.Commands;
using CGVStockApp.Application.Features.Products.Handlers;
using CGVStockApp.Domain.Entities;
using CGVStockApp.Infrastructure.Persistence.Context;

namespace CGVStockApp.Tests.Application.Features.Products
{
    public class CreateProductCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WithValidRequest_ReturnsProductResponse()
        {
            // Arrange
            var command = new CreateProductCommand
            {
                Name = "Test Product",
                PublicPrice = 100,
                WholesalePrice = 80,
                InitialStock = 10,
                AlertStock = 2
            };
            
            var mockContext = new Mock<StockAppDbContext>();
            var mockMapper = new Mock<IMapper>();
            var handler = new CreateProductCommandHandler(mockContext.Object, mockMapper.Object);
            
            // Act
            // var result = await handler.Handle(command, CancellationToken.None);
            
            // Assert
            // Assert.NotNull(result);
            // Assert.Equal("Test Product", result.Name);
        }
    }
}
```

---

**Documento siguiente:** Ejemplos de Frontend React + integración con CQRS API

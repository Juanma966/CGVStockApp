# 💻 IMPLEMENTACIÓN PRÁCTICA V2.0
## CGVStockApp - Código Listo para Copiar (Sin Repository Pattern)

---

## 📋 ÍNDICE

1. [Setup Inicial](#setup-inicial)
2. [IApplicationDbContext](#iapplicationdbcontext)
3. [Entidades Domain](#entidades-domain)
4. [DbContext + Configurations](#dbcontext--configurations)
5. [Feature Product Completa](#feature-product-completa)
6. [Feature Sale Completa](#feature-sale-completa)
7. [Feature Dashboard](#feature-dashboard)
8. [Configuración Global](#configuración-global)
9. [Controllers](#controllers)

---

## 🚀 SETUP INICIAL (Día 1-3)

```powershell
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

# Instalar dependencias
# En Domain
cd CGVStockApp.Domain
dotnet add package Microsoft.EntityFrameworkCore.Abstractions
cd ..

# En Application
cd CGVStockApp.Application
dotnet add package MediatR
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation
dotnet add package AutoMapper  # Limitado, solo si necesario
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

# Verificar
dotnet build
```

---

## 🔌 IApplicationDbContext

### Application/Common/Interfaces/IApplicationDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Domain.Entities;

namespace CGVStockApp.Application.Common.Interfaces
{
    /// <summary>
    /// Interfaz que expone los DbSets disponibles en la aplicación.
    /// Implementada por ApplicationDbContext en Infrastructure.
    /// Los Handlers de CQRS inyectan esta interfaz para acceder a datos.
    /// </summary>
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
        /// Persiste los cambios realizados al contexto en la base de datos.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
```

---

## 📦 ENTIDADES DOMAIN

### Domain/Entities/Product.cs

```csharp
using CGVStockApp.Domain.Common;

namespace CGVStockApp.Domain.Entities
{
    /// <summary>
    /// Representa un producto en el sistema.
    /// Contiene lógica de dominio que valida el estado del producto.
    /// </summary>
    public class Product : AuditableEntity, IAggregateRoot
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Código único del producto. Formato: AANNNN
        /// Ejemplo: FL0001 (Frutas - Limones - 0001)
        /// </summary>
        public string ProductCode { get; set; }
        
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal PublicPrice { get; set; }
        public decimal WholesalePrice { get; set; }
        
        /// <summary>
        /// Stock inicial (histórico, no cambia).
        /// </summary>
        public int InitialStock { get; set; }
        
        /// <summary>
        /// Stock disponible (se actualiza con cada venta).
        /// Fórmula: AvailableStock = InitialStock - QuantitySold
        /// </summary>
        public int AvailableStock { get; set; }
        
        /// <summary>
        /// Umbral de alerta para stock mínimo.
        /// Si AvailableStock <= AlertStock, aparece en reporte.
        /// </summary>
        public int AlertStock { get; set; }
        
        public int? CategoryId { get; set; }
        public Category Category { get; set; }
        
        public int? SubcategoryId { get; set; }
        public Subcategory Subcategory { get; set; }
        
        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        
        public bool IsActive { get; set; } = true;
        
        // ========== LÓGICA DE DOMINIO ==========
        
        /// <summary>
        /// Reduce el stock disponible.
        /// Lanza excepción si no hay stock suficiente.
        /// </summary>
        public void DecreaseStock(int quantity)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("La cantidad debe ser mayor a 0");
            
            if (quantity > AvailableStock)
                throw new InvalidOperationException(
                    $"Stock insuficiente. Disponible: {AvailableStock}, solicitado: {quantity}");
            
            AvailableStock -= quantity;
        }
        
        /// <summary>
        /// Aplica un cambio de precio porcentual.
        /// Ejemplo: percentageChange = 15 → +15%, = -10 → -10%
        /// </summary>
        public void UpdatePrices(decimal percentageChange)
        {
            if (percentageChange < -100)
                throw new InvalidOperationException("No se puede reducir más del 100%");
            
            decimal factor = 1 + (percentageChange / 100m);
            PublicPrice = Math.Round(PublicPrice * factor, 2);
            WholesalePrice = Math.Round(WholesalePrice * factor, 2);
        }
    }
}
```

### Domain/Entities/Sale.cs

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
        
        public CustomerType CustomerType { get; set; }  // Individual o Wholesale
        public PaymentMethodType PaymentMethod { get; set; }  // Efectivo, Tarjeta, Transferencia
        public decimal Total { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        
        public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
        
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

### Domain/Entities/AccountingMovement.cs

```csharp
using CGVStockApp.Domain.Common;
using CGVStockApp.Domain.Enums;

namespace CGVStockApp.Domain.Entities
{
    /// <summary>
    /// Representa un movimiento en el libro diario (contabilidad).
    /// Se crea automáticamente cuando ocurren eventos: Venta, Compra, Gasto.
    /// </summary>
    public class AccountingMovement : AuditableEntity
    {
        public int Id { get; set; }
        public DateTime MovementDate { get; set; }
        public AccountingMovementType Type { get; set; }  // Sale, Purchase, Expense
        public string Description { get; set; }
        public decimal Amount { get; set; }
        
        /// <summary>
        /// true = Entrada (ingresos), false = Salida (egresos)
        /// </summary>
        public bool IsIncome { get; set; }
        
        public int? SaleId { get; set; }
        public Sale Sale { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
```

### Domain/Enums/

```csharp
// Domain/Enums/AccountingMovementType.cs
namespace CGVStockApp.Domain.Enums
{
    public enum AccountingMovementType
    {
        Sale = 1,
        Purchase = 2,
        Expense = 3
    }
}

// Domain/Enums/CustomerType.cs
namespace CGVStockApp.Domain.Enums
{
    public enum CustomerType
    {
        Individual = 1,
        Wholesale = 2
    }
}

// Domain/Enums/PaymentMethodType.cs
namespace CGVStockApp.Domain.Enums
{
    public enum PaymentMethodType
    {
        Cash = 1,
        Card = 2,
        Transfer = 3
    }
}

// Domain/Enums/RoleType.cs
namespace CGVStockApp.Domain.Enums
{
    public enum RoleType
    {
        Admin = 1,
        Seller = 2
    }
}
```

---

## 💾 DbContext + Configurations

### Infrastructure/Persistence/Context/ApplicationDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Application.Common.Interfaces;
using CGVStockApp.Domain.Entities;
using CGVStockApp.Infrastructure.Persistence.Configurations;

namespace CGVStockApp.Infrastructure.Persistence.Context
{
    /// <summary>
    /// DbContext principal de la aplicación.
    /// Implementa IApplicationDbContext para que los Handlers puedan acceder.
    /// </summary>
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
            
            // Aplicar todas las configuraciones
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

### Infrastructure/Persistence/Configurations/ProductConfiguration.cs

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
            
            builder.Property(p => p.ProductCode)
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnName("product_code");
            
            builder.HasIndex(p => p.ProductCode).IsUnique();  // ✅ UNIQUE CONSTRAINT
            
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
            
            // Índices para búsqueda
            builder.HasIndex(p => p.Name);
            builder.HasIndex(p => p.CategoryId);
        }
    }
}
```

---

## 🎯 FEATURE PRODUCT COMPLETA

### Application/Features/Products/DTOs/

```csharp
// CreateProductRequest.cs
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

// ProductResponse.cs
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
}
```

### Application/Features/Products/Commands/CreateProductCommand.cs

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

### Application/Features/Products/Validators/CreateProductCommandValidator.cs

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
                .NotEmpty().WithMessage("El nombre es requerido")
                .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres");
            
            RuleFor(x => x.PublicPrice)
                .GreaterThan(0).WithMessage("El precio público debe ser mayor a 0");
            
            RuleFor(x => x.WholesalePrice)
                .GreaterThan(0).WithMessage("El precio mayorista debe ser mayor a 0");
            
            RuleFor(x => x.InitialStock)
                .GreaterThanOrEqualTo(0).WithMessage("El stock inicial no puede ser negativo");
            
            RuleFor(x => x.AlertStock)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(x => x.InitialStock)
                .WithMessage("El stock de alerta debe ser mayor a 0 y menor o igual al stock inicial");
        }
    }
}
```

### Application/Features/Products/Handlers/CreateProductCommandHandler.cs

```csharp
using MediatR;
using AutoMapper;
using CGVStockApp.Application.Common.Interfaces;
using CGVStockApp.Application.Features.Products.Commands;
using CGVStockApp.Application.Features.Products.DTOs;
using CGVStockApp.Domain.Entities;

namespace CGVStockApp.Application.Features.Products.Handlers
{
    public class CreateProductCommandHandler 
        : IRequestHandler<CreateProductCommand, ProductResponse>
    {
        private readonly IApplicationDbContext _context;  // ✅ Inyectado directamente
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
            string productCode = await GenerateProductCode(request.CategoryId, request.SubcategoryId, cancellationToken);
            
            // MAPEAR con AutoMapper (solo porque hay lógica de transformación)
            var product = _mapper.Map<Product>(request);
            product.ProductCode = productCode;
            product.AvailableStock = request.InitialStock;
            
            // PERSISTIR con DbContext DIRECTO (sin Repository)
            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);
            
            // RETORNAR sin AutoMapper (manual)
            return new ProductResponse
            {
                Id = product.Id,
                ProductCode = product.ProductCode,
                Name = product.Name,
                Description = product.Description,
                PublicPrice = product.PublicPrice,
                WholesalePrice = product.WholesalePrice,
                InitialStock = product.InitialStock,
                AvailableStock = product.AvailableStock,
                AlertStock = product.AlertStock
            };
        }
        
        private async Task<string> GenerateProductCode(
            int? categoryId,
            int? subcategoryId,
            CancellationToken cancellationToken)
        {
            string categoryLetter = "P";
            string subcategoryLetter = "0";
            
            if (categoryId.HasValue)
            {
                var category = await _context.Categories
                    .FindAsync(new object[] { categoryId.Value }, cancellationToken: cancellationToken);
                if (category != null)
                    categoryLetter = category.Name[0].ToString().ToUpper();
            }
            
            if (subcategoryId.HasValue)
            {
                var subcategory = await _context.Subcategories
                    .FindAsync(new object[] { subcategoryId.Value }, cancellationToken: cancellationToken);
                if (subcategory != null)
                    subcategoryLetter = subcategory.Name[0].ToString().ToUpper();
            }
            
            // Obtener próximo número
            var lastProduct = _context.Products
                .Where(p => p.ProductCode.StartsWith(categoryLetter + subcategoryLetter))
                .OrderByDescending(p => p.ProductCode)
                .FirstOrDefault();
            
            int nextNumber = 1;
            if (lastProduct != null && int.TryParse(lastProduct.ProductCode.Substring(2), out var num))
                nextNumber = num + 1;
            
            return $"{categoryLetter}{subcategoryLetter}{nextNumber:D4}";
        }
    }
}
```

### Application/Features/Products/Queries/GetProductsQuery.cs

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

### Application/Features/Products/Handlers/GetProductsQueryHandler.cs

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Application.Common.Interfaces;
using CGVStockApp.Application.Features.Products.Queries;
using CGVStockApp.Application.Features.Products.DTOs;

namespace CGVStockApp.Application.Features.Products.QueryHandlers
{
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
            // SIN AutoMapper - Select() directo a DTO
            return await _context.Products
                .Where(p => !request.OnlyActive || p.IsActive)
                .Where(p => !request.CategoryId.HasValue || p.CategoryId == request.CategoryId)
                .Where(p => !request.SubcategoryId.HasValue || p.SubcategoryId == request.SubcategoryId)
                .Select(p => new ProductResponse
                {
                    Id = p.Id,
                    ProductCode = p.ProductCode,
                    Name = p.Name,
                    Description = p.Description,
                    PublicPrice = p.PublicPrice,
                    WholesalePrice = p.WholesalePrice,
                    InitialStock = p.InitialStock,
                    AvailableStock = p.AvailableStock,
                    AlertStock = p.AlertStock,
                    CategoryName = p.Category.Name,
                    SubcategoryName = p.Subcategory.Name,
                    IsActive = p.IsActive
                })
                .OrderBy(p => p.CategoryName)
                .ThenBy(p => p.SubcategoryName)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
```

### Application/Features/Products/Queries/GetMinimumStockProductsQuery.cs

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

### Application/Features/Products/Handlers/GetMinimumStockProductsQueryHandler.cs

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Application.Common.Interfaces;
using CGVStockApp.Application.Features.Products.Queries;
using CGVStockApp.Application.Features.Products.DTOs;

namespace CGVStockApp.Application.Features.Products.QueryHandlers
{
    public class GetMinimumStockProductsQueryHandler 
        : IRequestHandler<GetMinimumStockProductsQuery, List<ProductResponse>>
    {
        private readonly IApplicationDbContext _context;
        
        public GetMinimumStockProductsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<ProductResponse>> Handle(
            GetMinimumStockProductsQuery request,
            CancellationToken cancellationToken)
        {
            return await _context.Products
                .Where(p => p.AvailableStock <= p.AlertStock && p.IsActive)
                .Select(p => new ProductResponse
                {
                    Id = p.Id,
                    ProductCode = p.ProductCode,
                    Name = p.Name,
                    AvailableStock = p.AvailableStock,
                    AlertStock = p.AlertStock,
                    CategoryName = p.Category.Name,
                    SubcategoryName = p.Subcategory.Name
                })
                .OrderBy(p => p.CategoryName)
                .ThenBy(p => p.SubcategoryName)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
```

### Application/Features/Products/Mappings/ProductMappingProfile.cs

```csharp
using AutoMapper;
using CGVStockApp.Application.Features.Products.Commands;
using CGVStockApp.Domain.Entities;

namespace CGVStockApp.Application.Features.Products.Mappings
{
    /// <summary>
    /// Mapeos para Products. 
    /// NOTA: No mapeamos Product → ProductResponse aquí.
    /// Usamos Select() directo en Queries para mejor performance.
    /// </summary>
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            // SOLO mapeos complejos (Command → Entity)
            CreateMap<CreateProductCommand, Product>()
                .ForMember(dest => dest.ProductCode, opt => opt.Ignore())
                .ForMember(dest => dest.AvailableStock, 
                    opt => opt.MapFrom(src => src.InitialStock));
        }
    }
}
```

---

## 💼 FEATURE SALE COMPLETA

### Application/Features/Sales/Commands/CreateSaleCommand.cs

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

### Application/Features/Sales/Handlers/CreateSaleCommandHandler.cs

```csharp
using MediatR;
using CGVStockApp.Application.Common.Interfaces;
using CGVStockApp.Application.Features.Sales.Commands;
using CGVStockApp.Application.Features.Sales.DTOs;
using CGVStockApp.Domain.Entities;
using CGVStockApp.Domain.Enums;

namespace CGVStockApp.Application.Features.Sales.Handlers
{
    public class CreateSaleCommandHandler 
        : IRequestHandler<CreateSaleCommand, SaleResponse>
    {
        private readonly IApplicationDbContext _context;  // ✅ Directo
        
        public CreateSaleCommandHandler(IApplicationDbContext context)
        {
            _context = context;
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
            
            // Procesar detalles
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
                
                // Obtener precio según tipo de cliente
                var appliedPrice = sale.GetAppliedPrice(product);
                
                // Crear detalle
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
            
            // Registrar en libro diario (IMPORTANTE para Dashboard)
            var accountingMovement = new AccountingMovement
            {
                MovementDate = sale.SaleDate,
                Type = AccountingMovementType.Sale,
                Description = $"Venta#{sale.Id}",
                Amount = sale.Total,
                IsIncome = true,  // Entrada
                SaleId = sale.Id,
                UserId = request.UserId
            };
            
            _context.Sales.Add(sale);
            _context.AccountingMovements.Add(accountingMovement);
            await _context.SaveChangesAsync(cancellationToken);
            
            return new SaleResponse
            {
                Id = sale.Id,
                UserId = sale.UserId,
                CustomerType = sale.CustomerType.ToString(),
                PaymentMethod = sale.PaymentMethod.ToString(),
                Total = sale.Total,
                SaleDate = sale.SaleDate,
                Details = sale.Details.Select(d => new SaleDetailResponse
                {
                    ProductId = d.ProductId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Subtotal = d.Subtotal
                }).ToList()
            };
        }
    }
}
```

---

## 📊 FEATURE DASHBOARD

### Application/Features/Dashboard/Queries/GetDailySalesQuery.cs

```csharp
using MediatR;
using CGVStockApp.Application.Features.Dashboard.DTOs;

namespace CGVStockApp.Application.Features.Dashboard.Queries
{
    public class GetDailySalesQuery : IRequest<DashboardResponse>
    {
        public DateTime Date { get; set; } = DateTime.Today;
    }
}
```

### Application/Features/Dashboard/Handlers/GetDailySalesQueryHandler.cs

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Application.Common.Interfaces;
using CGVStockApp.Application.Features.Dashboard.Queries;
using CGVStockApp.Application.Features.Dashboard.DTOs;

namespace CGVStockApp.Application.Features.Dashboard.QueryHandlers
{
    public class GetDailySalesQueryHandler 
        : IRequestHandler<GetDailySalesQuery, DashboardResponse>
    {
        private readonly IApplicationDbContext _context;
        
        public GetDailySalesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<DashboardResponse> Handle(
            GetDailySalesQuery request,
            CancellationToken cancellationToken)
        {
            var startOfDay = request.Date.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);
            
            // SIN AutoMapper - Select() directo
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
            
            var incomes = movements.Where(m => m.IsIncome).Sum(m => m.Amount);
            var expenses = movements.Where(m => !m.IsIncome).Sum(m => m.Amount);
            
            return new DashboardResponse
            {
                Period = "Diario",
                Date = request.Date,
                TotalIncomes = incomes,
                TotalExpenses = expenses,
                Balance = incomes - expenses,
                Movements = movements
            };
        }
    }
}
```

### Application/Features/Dashboard/DTOs/DashboardResponse.cs

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

### Application/DependencyInjection.cs

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
            
            // AutoMapper (solo para los mappings que definimos)
            services.AddAutoMapper(typeof(ProductMappingProfile).Assembly);
            
            return services;
        }
    }
}
```

### Infrastructure/DependencyInjection.cs

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CGVStockApp.Application.Common.Interfaces;
using CGVStockApp.Infrastructure.Persistence.Context;
using CGVStockApp.Infrastructure.Services;

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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
            
            // Registrar interfaz ✅ (IMPORTANTE: Sin Repository Pattern)
            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());
            
            // Services
            services.AddScoped<JwtTokenService>();
            services.AddScoped<PasswordHashService>();
            services.AddScoped<PdfGenerationService>();
            
            return services;
        }
    }
}
```

### Api/Program.cs

```csharp
using CGVStockApp.Application;
using CGVStockApp.Infrastructure;
using CGVStockApp.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add services to DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add controllers and Swagger
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

// Migrations y Seeding
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    // Seeding...
}

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

---

## 🎮 CONTROLLERS

### Api/Controllers/ProductController.cs

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
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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
        
        [HttpGet("minimum-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetMinimumStock()
        {
            var query = new GetMinimumStockProductsQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // Implementar GetProductByIdQuery
            return Ok();
        }
    }
}
```

---

**Próximo documento:** Roadmap revisado v2.0 (Días 1-47)

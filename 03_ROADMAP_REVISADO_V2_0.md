# 📅 ROADMAP REVISADO V2.0 - CGVStockApp
## Cronograma Detallado: 42-47 Días (Ganamos 3 días)

**Actualizado:** Junio 2026  
**Versión:** 2.0 (Revisada)  
**Cambios Aplicados:** Sin Repository Pattern, AutoMapper limitado, Custom User+JWT  
**Status:** ✅ FINAL

---

## 🎯 RESUMEN DE CAMBIOS APLICADOS

### Impacto en Cronograma

| Cambio | Días Ahorrados | Razón |
|--------|---|---|
| Eliminar Repository Pattern | -2 días | Menos código, EF Core directo |
| AutoMapper limitado | -1 día | Select() en queries, sin mappings innecesarios |
| Custom User + JWT | 0 días | Mantener para agilidad |
| **TOTAL** | **-3 días** | ✅ Ganados para testing/refinamiento |

### Nuevo Cronograma

**Original:** 45-50 días  
**Nuevo:** 42-47 días

---

## 📊 DESGLOSE POR FASES (42-47 DÍAS)

### **FASE 0: SETUP INICIAL (Días 1-3) | 15 horas**

#### Día 1: Instalación
- [ ] .NET 9 SDK (actualizar de 8 a 9)
- [ ] PostgreSQL 16 + pgAdmin
- [ ] VSCode + extensiones C#
- [ ] Verificar: `dotnet --version` → 9.0.x

#### Día 2: Crear Solución
- [ ] Estructura: `dotnet new sln -n CGVStockApp`
- [ ] 4 proyectos:
  - CGVStockApp.Domain (classlib)
  - CGVStockApp.Application (classlib)
  - CGVStockApp.Infrastructure (classlib)
  - CGVStockApp.Api (webapi)
- [ ] Referencias entre proyectos ✅
- [ ] Instalar NuGet: MediatR, FluentValidation, EF Core, Npgsql, JWT, iText7

#### Día 3: Verificación y Git
- [ ] `dotnet build` → sin errores
- [ ] Crear repositorio GitHub
- [ ] Commit inicial
- [ ] Carpetas lisas

#### ✅ CHECKLIST FASE 0
- [ ] .NET 9 instalado
- [ ] PostgreSQL funcionando
- [ ] Solución con 4 proyectos
- [ ] Referencias correctas
- [ ] Paquetes NuGet instalados
- [ ] `dotnet build` successful

---

### **FASE 1: BASE DE DATOS (Días 4-9) | 30 horas**

#### Día 4-5: Domain Layer (Entidades)
**Crear:**
- [ ] User.cs + Role.cs
- [ ] Category.cs + Subcategory.cs
- [ ] Product.cs (con lógica DecreaseStock, UpdatePrices)
- [ ] Sale.cs + SaleDetail.cs
- [ ] StockMovement.cs
- [ ] AccountingMovement.cs (Libro Diario) ✅ MANTENER
- [ ] Excepciones (InsufficientStockException, etc)
- [ ] Enums (RoleType, CustomerType, PaymentMethodType, etc)
- [ ] AuditableEntity.cs + IAggregateRoot.cs

**Estructura:**
```
Domain/
├── Entities/ (9 entidades)
├── Enums/ (5 enums)
├── Common/ (AuditableEntity, IAggregateRoot)
└── Exceptions/
```

#### Día 6-7: Infrastructure Layer (DbContext + Configurations)
**Crear:**
- [ ] IApplicationDbContext.cs (en Application/Common/Interfaces) ✅ NUEVA INTERFAZ
- [ ] ApplicationDbContext.cs (Implementa IApplicationDbContext)
- [ ] EntityTypeConfigurations para cada entidad:
  - UserConfiguration.cs
  - RoleConfiguration.cs
  - ProductConfiguration.cs (con UNIQUE constraint en ProductCode)
  - CategoryConfiguration.cs
  - SubcategoryConfiguration.cs
  - SaleConfiguration.cs
  - SaleDetailConfiguration.cs
  - StockMovementConfiguration.cs
  - AccountingMovementConfiguration.cs

**IMPORTANTE:**
- [ ] ProductCode tiene restricción UNIQUE ✅
- [ ] Índices para búsqueda (Name, CategoryId)
- [ ] Auditoría (CreatedAt, ModifiedAt)

#### Día 8-9: Migraciones y Seeding
- [ ] Crear migración inicial: `dotnet ef migrations add InitialCreate`
- [ ] Actualizar BD: `dotnet ef database update`
- [ ] Verificar en pgAdmin: todas las tablas creadas
- [ ] Seeding de datos iniciales:
  - Crear roles: Admin, Seller
  - Crear usuario admin de prueba
- [ ] Verificar integridad referencial

#### ✅ CHECKLIST FASE 1
- [ ] Todas entidades en Domain
- [ ] DbContext implementando IApplicationDbContext
- [ ] Todas las configurations
- [ ] Migraciones ejecutadas
- [ ] BD en PostgreSQL con todas las tablas
- [ ] Datos de seed (roles, usuario admin)

---

### **FASE 2: INFRASTRUCTURE SERVICES (Días 10-12) | 15 horas**

**⚠️ SIN REPOSITORY PATTERN - Acceso directo via IApplicationDbContext**

#### Día 10: Servicios de Infraestructura
- [ ] JwtTokenService.cs
  - GenerateToken(User, secretKey, expirationMinutes)
  - Crear JWT con claims
- [ ] PasswordHashService.cs
  - HashPassword(string)
  - VerifyPassword(string, string)
- [ ] PdfGenerationService.cs
  - GeneratePdfStockMinimo(List<Product>)
  - Usando iText7

#### Día 11: DependencyInjection
- [ ] Infrastructure/DependencyInjection.cs
  - Registrar DbContext
  - Registrar IApplicationDbContext ✅
  - Registrar servicios (JWT, Password, PDF)
  - NO registrar Repositories (eliminados)

#### Día 12: Configuración Program.cs
- [ ] Completar Api/Program.cs
- [ ] Registrar servicios desde Infrastructure
- [ ] JWT configuration
- [ ] CORS setup
- [ ] Migrations y seeding en startup

#### ✅ CHECKLIST FASE 2
- [ ] JwtTokenService funcionando
- [ ] PasswordHashService hash+verify funcional
- [ ] PdfGenerationService listo
- [ ] Infrastructure/DependencyInjection.cs completo
- [ ] Api/Program.cs con todo configurado

---

### **FASE 3: APPLICATION STRUCTURE (Días 13-15) | 15 horas**

#### Día 13: Common Base
- [ ] Common/Interfaces/IApplicationDbContext.cs ✅
- [ ] Common/Behaviors/ValidationBehavior.cs
- [ ] Common/Behaviors/LoggingBehavior.cs
- [ ] Common/Behaviors/PerformanceBehavior.cs
- [ ] Common/Exceptions/ (ApplicationException, ValidationException, NotFoundException)

#### Día 14: Features Folder Structure
- [ ] Crear Features/ con subcarpetas:
  - Authentication/
  - Products/
  - Categories/
  - Subcategories/
  - Sales/
  - Users/
  - Roles/
  - Dashboard/

**Estructura por Feature (ejemplo Products):**
```
Features/Products/
├── Commands/
├── Handlers/
├── Queries/
├── QueryHandlers/
├── DTOs/
├── Validators/
└── Mappings/ (SOLO si es necesario)
```

#### Día 15: DependencyInjection
- [ ] Application/DependencyInjection.cs
  - AddMediatR()
  - AddFluentValidation()
  - AddAutoMapper() (limitado)
  - AddPipelineBehaviors()

#### ✅ CHECKLIST FASE 3
- [ ] IApplicationDbContext definida
- [ ] Common/ implementado
- [ ] Features/ carpetas creadas
- [ ] Application/DependencyInjection.cs completo

---

### **FASE 4: CQRS FEATURES (Días 16-35) | 80 horas**

**ORDEN DE IMPLEMENTACIÓN (crítico):**

#### **Subproducto 1: Authentication (Días 16-17) | 10 horas**

**Crear:**
- [ ] LoginCommand
- [ ] LoginCommandHandler
- [ ] LoginCommandValidator
- [ ] LoginRequest DTO
- [ ] LoginResponse DTO

**Lógica:**
- [ ] Validar credenciales contra User en BD
- [ ] Generar JWT token
- [ ] Retornar token + info usuario

#### **Subproducto 2: Products (Días 18-24) | 35 horas**

**COMMANDS:**
- [ ] CreateProductCommand
  - ✅ Genera ProductCode automático (AANNNN)
  - ✅ Mapea con AutoMapper (transformación compleja)
- [ ] UpdateProductCommand
- [ ] DeleteProductCommand
- [ ] UpdatePriceCommand
  - ✅ Aplica porcentaje a todos productos de categoría
  - ✅ Modifica PublicPrice + WholesalePrice

**HANDLERS:**
- [ ] CreateProductCommandHandler
  - Inyecta: IApplicationDbContext (directo)
  - Valida categoría/subcategoría existe
  - Genera ProductCode
  - Mapea Command → Product (AutoMapper)
  - Persiste: _context.Products.Add() + SaveChangesAsync()
  
- [ ] UpdateProductCommandHandler
- [ ] DeleteProductCommandHandler
- [ ] UpdatePriceCommandHandler

**QUERIES:**
- [ ] GetProductsQuery (lista con filtros)
- [ ] GetProductByIdQuery
- [ ] SearchProductsQuery (autocompletado)
- [ ] GetMinimumStockProductsQuery (para PDF)

**HANDLERS (Sin AutoMapper - Select() directo):**
- [ ] GetProductsQueryHandler
  - Select() → ProductResponse (sin AutoMapper)
  - Ordenamiento: Category → Subcategory → Name
  
- [ ] GetProductByIdQueryHandler
- [ ] SearchProductsQueryHandler
- [ ] GetMinimumStockProductsQueryHandler

**DTOs:**
- [ ] CreateProductRequest
- [ ] UpdateProductRequest
- [ ] ProductResponse
- [ ] SearchProductResponse

**VALIDATORS:**
- [ ] CreateProductCommandValidator
- [ ] UpdateProductCommandValidator

**MAPPINGS:**
- [ ] ProductMappingProfile
  - ✅ SOLO: CreateProductCommand → Product
  - ❌ NO: Product → ProductResponse (usar Select())

#### **Subproducto 3: Categories (Días 25-27) | 15 horas**

- [ ] CreateCategoryCommand + Handler + Validator
- [ ] UpdateCategoryCommand + Handler
- [ ] DeleteCategoryCommand + Handler
- [ ] GetCategoriesQuery + Handler
- [ ] GetCategoryByIdQuery + Handler
- [ ] DTOs, Validators, Mappings

#### **Subproducto 4: Subcategories (Días 28-29) | 10 horas**

- [ ] CRUD completo (similar a Categories)
- [ ] GetSubcategoriesByCategory Query

#### **Subproducto 5: Sales (Días 30-33) | 20 horas**

**COMMAND (MÁS COMPLEJO):**
- [ ] CreateSaleCommand
  - Lista de detalles (ProductId, Quantity)
  - Inyecta: IApplicationDbContext (directo)
  
**HANDLER:**
- [ ] CreateSaleCommandHandler
  - ✅ Valida stock disponible
  - ✅ Llama product.DecreaseStock() (lógica dominio)
  - ✅ Crea AccountingMovement automáticamente ✅
  - ✅ Calcula precio según CustomerType
  - ✅ Registra movimiento de stock
  - ✅ Persiste: Sales + Detalles + AccountingMovement + StockMovement

**QUERIES:**
- [ ] GetSalesQuery
- [ ] GetSaleByIdQuery
- [ ] GetSalesByPeriodQuery (para dashboard)

**DTOs, Validators, etc.**

#### **Subproducto 6: Users (Días 34-35) | 10 horas**

- [ ] CreateUserCommand + Handler
- [ ] UpdateUserCommand + Handler
- [ ] DeleteUserCommand + Handler
- [ ] ChangePasswordCommand + Handler
- [ ] GetUsersQuery + Handler
- [ ] DTOs, Validators, Mappings

#### ✅ CHECKLIST FASE 4
- [ ] Todas las features implementadas
- [ ] Commands con Handler para cada operación
- [ ] Queries con Handler para cada lectura
- [ ] Validadores en cada Command
- [ ] DTOs específicos por Feature
- [ ] ProductCode generación automática ✅
- [ ] Stock deduction funcional ✅
- [ ] AccountingMovement registrado ✅
- [ ] Select() sin AutoMapper en Queries ✅
- [ ] AutoMapper solo en Commands complejos ✅

---

### **FASE 5: API CONTROLLERS (Días 36-38) | 15 horas**

#### Día 36-37: Crear Controllers
- [ ] AuthController (Login)
- [ ] ProductController (CRUD + búsqueda + mínimo stock)
- [ ] CategoryController (CRUD)
- [ ] SubcategoryController (CRUD)
- [ ] SalesController (Create + queries)
- [ ] UserController (CRUD)

**Cada endpoint:**
- [ ] Convierte Request → Command/Query
- [ ] Envía: `await _mediator.Send(command)`
- [ ] Maneja excepciones
- [ ] Retorna respuesta HTTP adecuada

#### Día 38: ErrorHandling + Middleware
- [ ] ErrorHandlingMiddleware
  - Atrapa ValidationException → 400
  - Atrapa NotFoundException → 404
  - Atrapa DomainException → 400
  - Atrapa Exception → 500
- [ ] Registrar middleware en Program.cs
- [ ] Retornar JSON con mensaje de error

#### ✅ CHECKLIST FASE 5
- [ ] Todos los controllers creados
- [ ] Endpoints mapeados correctamente
- [ ] [Authorize] en endpoints admin
- [ ] Roles validando (Admin vs Seller)
- [ ] Errores manejados centralizado

---

### **FASE 6: FRONTEND REACT (Días 39-42) | 20 horas**

#### Día 39: Setup React
- [ ] `npx create-react-app frontend`
- [ ] `npm install axios react-router-dom zustand`
- [ ] Estructura carpetas:
  ```
  src/
  ├── pages/
  ├── components/
  ├── services/
  └── store/
  ```

#### Día 40: Authentication
- [ ] Login page
- [ ] API service (axios)
- [ ] Zustand store (token + usuario)
- [ ] PrivateRoute wrapper
- [ ] Navbar con logout

#### Día 41: Admin Vistas
- [ ] Products CRUD
- [ ] Categories CRUD
- [ ] Dashboard (tabla movimientos)
- [ ] Stock mínimo (tabla + botón PDF)

#### Día 42: Vendor Vistas + Modal
- [ ] Menu productos (búsqueda + tabla)
- [ ] Modal de venta (producto, cantidad, cliente, pago)
- [ ] Validación stock insuficiente
- [ ] Mensaje "Venta registrada" (3 seg)
- [ ] Responsivo (mobile/tablet)

#### ✅ CHECKLIST FASE 6
- [ ] React conectada a API
- [ ] Login funcional
- [ ] CRUD productos
- [ ] Modal venta con validaciones
- [ ] Dashboard mostrando movimientos
- [ ] PDF descargable
- [ ] Responsivo en todos los dispositivos

---

### **FASE 7: TESTING & DEPLOYMENT (Días 43-47) | 25 horas**

#### Días 43-45: Testing Manual
- [ ] Checklist de 40+ casos de uso
- [ ] Testing en navegadores (Chrome, Firefox, Edge)
- [ ] Testing en móvil (responsive)
- [ ] Corrección de bugs
- [ ] Validaciones funcionales

**Testing checklist (muestra):**
- [ ] Crear producto con ProductCode automático
- [ ] Venta descuenta stock correctamente
- [ ] Dashboard muestra movimientos
- [ ] PDF descarga correctamente
- [ ] Login con JWT funciona
- [ ] Roles Admin vs Seller se respetan

#### Días 46-47: Deployment
- [ ] Setup Railway.app (recomendado)
  - Conectar GitHub
  - Configurar variables entorno
  - Deploy automático
- [ ] BD en producción
  - Crear BD en Railway
  - Ejecutar migraciones
- [ ] Frontend deploy (static site)
- [ ] Verificar URL pública funciona
- [ ] Monitoreo básico

#### ✅ CHECKLIST FASE 7
- [ ] Todas funcionalidades testeadas
- [ ] Deployment exitoso
- [ ] App accesible desde URL pública
- [ ] BD producción funcionando
- [ ] Usuarios pueden acceder

---

## 📊 ESTRUCTURA FINAL CONFIRMADA

```
CGVStockApp/
│
├── CGVStockApp.Domain/
│   ├── Entities/ (9 entidades)
│   ├── Enums/ (5 enums)
│   ├── Common/ (AuditableEntity, IAggregateRoot)
│   └── Exceptions/
│
├── CGVStockApp.Application/
│   ├── Features/ (8 features × ~15 archivos cada una)
│   ├── Common/
│   │   ├── Interfaces/ (IApplicationDbContext) ✅ NUEVO
│   │   ├── Behaviors/ (Validation, Logging, Performance)
│   │   ├── Exceptions/
│   │   └── Constants/
│   └── DependencyInjection.cs
│
├── CGVStockApp.Infrastructure/
│   ├── Persistence/
│   │   ├── Context/ (ApplicationDbContext)
│   │   ├── Configurations/ (9 IEntityTypeConfiguration)
│   │   └── Migrations/
│   ├── Services/ (Jwt, Password, Pdf)
│   └── DependencyInjection.cs
│
├── CGVStockApp.Api/
│   ├── Controllers/ (7 controllers)
│   ├── Middlewares/ (ErrorHandling)
│   ├── Program.cs
│   └── appsettings.json
│
└── frontend/ (React)
    ├── src/
    └── package.json
```

---

## 🎯 FUNCIONALIDADES FINALES

### Admin
- ✅ Crear/Editar/Eliminar Usuarios
- ✅ CRUD Categorías
- ✅ CRUD Subcategorías
- ✅ CRUD Productos (ProductCode automático)
- ✅ Modificar Precios (porcentaje)
- ✅ Stock Mínimo (tabla + PDF descargable)
- ✅ Dashboard (movimientos diarios/semanales/mensuales)
- ✅ Ver Menú Productos

### Vendedor
- ✅ Ver Menú Productos (búsqueda + tabla)
- ✅ Registrar Venta (modal con validación stock)
- ✅ Mensaje confirmación venta

### Ambos
- ✅ Autenticación JWT
- ✅ Menú principal

---

## 📈 MÉTRICAS FINALES

| Métrica | Valor |
|---------|-------|
| **Duración** | 42-47 días |
| **Tiempo ahorrado** | 3 días |
| **Entidades Domain** | 9 |
| **Features** | 8 |
| **Commands** | ~20 |
| **Queries** | ~15 |
| **Controllers** | 7 |
| **Líneas de código** | ~15,000 |

---

## ✅ CAMBIOS V2.0 CONFIRMADOS

| Cambio | Status |
|--------|--------|
| Sin Repository Pattern | ✅ APLICADO |
| IApplicationDbContext directo | ✅ APLICADO |
| AutoMapper limitado | ✅ APLICADO |
| Select() en Queries | ✅ APLICADO |
| AccountingMovement | ✅ MANTENER |
| ProductCode UNIQUE | ✅ APLICADO |
| Custom User + JWT | ✅ MANTENER |
| CQRS corrección conceptual | ✅ APLICADO |

---

## 🎓 CONCLUSIÓN

**CGVStockApp v2.0:**
- ✅ Arquitectura limpia: CQRS + MediatR + Clean Architecture
- ✅ Eficiente: -3 días en cronograma
- ✅ Funcional: 100% de requisitos originales
- ✅ Agil: Sin complejidades innecesarias
- ✅ Testeable: Fácil de mantener y extender

**Status:** READY TO DEVELOP 🚀

---

**Documentos disponibles:**
1. ✅ 00_CAMBIOS_V2_0.md - Justificación de cambios
2. ✅ 01_ARQUITECTURA_REVISADA_V2_0.md - Arquitectura final
3. ✅ 02_IMPLEMENTACION_REVISADA_V2_0.md - Código práctico
4. ✅ 03_ROADMAP_REVISADO_V2_0.md - Este documento

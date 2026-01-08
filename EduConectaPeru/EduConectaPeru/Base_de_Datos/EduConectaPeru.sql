USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'EduConectaPeru')
BEGIN
    ALTER DATABASE EduConectaPeru SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE EduConectaPeru;
END
GO

CREATE DATABASE EduConectaPeru;
GO

USE EduConectaPeru;
GO

-- =============================================
-- TABLA: Users (Usuarios del sistema)
-- =============================================
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Role NVARCHAR(50) NOT NULL CHECK (Role IN ('Administrador', 'Secretaria', 'Apoderado')),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- =============================================
-- TABLA: LegalGuardians (Apoderados) - CON IsActive
-- =============================================
CREATE TABLE LegalGuardians (
    LegalGuardianId INT IDENTITY(1,1) PRIMARY KEY,
    DNI NVARCHAR(8) NOT NULL UNIQUE,
    Nombre NVARCHAR(100) NOT NULL,
    Apellido NVARCHAR(100) NOT NULL,
    Direccion NVARCHAR(200) NOT NULL,
    Telefono NVARCHAR(15) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- =============================================
-- TABLA: Students (Estudiantes)
-- =============================================
CREATE TABLE Students (
    StudentId INT IDENTITY(1,1) PRIMARY KEY,
    DNI NVARCHAR(8) NOT NULL UNIQUE,
    Nombre NVARCHAR(100) NOT NULL,
    Apellido NVARCHAR(100) NOT NULL,
    FechaNacimiento DATE NOT NULL,
    Direccion NVARCHAR(200) NOT NULL,
    Telefono NVARCHAR(15) NULL,
    Email NVARCHAR(100) NULL,
    LegalGuardianId INT NOT NULL,
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (LegalGuardianId) REFERENCES LegalGuardians(LegalGuardianId)
);
GO

-- =============================================
-- TABLA: Banks (Bancos) - CON IsActive
-- =============================================
CREATE TABLE Banks (
    BankId INT IDENTITY(1,1) PRIMARY KEY,
    BankName NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- =============================================
-- TABLA: PaymentStatus (Estados de Pago) - SIN IsActive
-- =============================================
CREATE TABLE PaymentStatus (
    StatusId INT IDENTITY(1,1) PRIMARY KEY,
    StatusName NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- =============================================
-- TABLA: PaymentTypes (Tipos de Pago) - CON IsActive AGREGADO
-- =============================================
CREATE TABLE PaymentTypes (
    PaymentTypeId INT IDENTITY(1,1) PRIMARY KEY,
    TypeName NVARCHAR(50) NOT NULL UNIQUE,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- =============================================
-- TABLA: TipoCurso (Tipos de Curso)
-- =============================================
CREATE TABLE TipoCurso (
    TipoCursoId INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(500) NULL
);
GO

-- =============================================
-- TABLA: Docentes (Profesores) - CON IsActive
-- =============================================
CREATE TABLE Docentes (
    DocenteId INT IDENTITY(1,1) PRIMARY KEY,
    DNI NVARCHAR(8) NOT NULL UNIQUE,
    Nombre NVARCHAR(100) NOT NULL,
    Apellido NVARCHAR(100) NOT NULL,
    Especialidad NVARCHAR(100) NOT NULL,
    Telefono NVARCHAR(15) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    FechaContratacion DATE NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- =============================================
-- TABLA: GradoSecciones (Grados y Secciones)
-- =============================================
CREATE TABLE GradoSecciones (
    GradoSeccionId INT IDENTITY(1,1) PRIMARY KEY,
    Grado NVARCHAR(50) NOT NULL,
    Seccion NVARCHAR(10) NOT NULL,
    AnioEscolar INT NOT NULL,
    Capacidad INT NOT NULL CHECK (Capacidad >= 1 AND Capacidad <= 50),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_GradoSeccion UNIQUE (Grado, Seccion, AnioEscolar)
);
GO

-- =============================================
-- TABLA: Horarios - SIN DocenteId (solo tiene Curso)
-- =============================================
CREATE TABLE Horarios (
    HorarioId INT IDENTITY(1,1) PRIMARY KEY,
    GradoSeccionId INT NOT NULL,
    Curso NVARCHAR(100) NOT NULL,
    DiaSemana NVARCHAR(20) NOT NULL,
    HoraInicio TIME NOT NULL,
    HoraFin TIME NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (GradoSeccionId) REFERENCES GradoSecciones(GradoSeccionId)
);
GO

-- =============================================
-- TABLA: AsignacionDocente (Asignación de Docentes)
-- =============================================
CREATE TABLE AsignacionDocente (
    AsignacionId INT IDENTITY(1,1) PRIMARY KEY,
    DocenteId INT NOT NULL,
    HorarioId INT NOT NULL,
    FechaAsignacion DATE NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (DocenteId) REFERENCES Docentes(DocenteId),
    FOREIGN KEY (HorarioId) REFERENCES Horarios(HorarioId)
);
GO

-- =============================================
-- TABLA: Matriculas
-- =============================================
CREATE TABLE Matriculas (
    MatriculaId INT IDENTITY(1,1) PRIMARY KEY,
    StudentId INT NOT NULL,
    LegalGuardianId INT NOT NULL,
    GradoSeccionId INT NOT NULL,
    AnioEscolar INT NOT NULL,
    FechaMatricula DATE NOT NULL DEFAULT GETDATE(),
    MontoMatricula DECIMAL(10,2) NOT NULL,
    Estado NVARCHAR(50) NOT NULL DEFAULT 'Activa',
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
    FOREIGN KEY (LegalGuardianId) REFERENCES LegalGuardians(LegalGuardianId),
    FOREIGN KEY (GradoSeccionId) REFERENCES GradoSecciones(GradoSeccionId)
);
GO

-- =============================================
-- TABLA: Quotas (Cuotas/Pensiones)
-- =============================================
CREATE TABLE Quotas (
    QuotaId INT IDENTITY(1,1) PRIMARY KEY,
    MatriculaId INT NOT NULL,
    StudentId INT NOT NULL,
    Mes NVARCHAR(20) NOT NULL,
    Anio INT NOT NULL,
    Monto DECIMAL(10,2) NOT NULL,
    FechaVencimiento DATE NOT NULL,
    PaymentStatusId INT NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (MatriculaId) REFERENCES Matriculas(MatriculaId),
    FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
    FOREIGN KEY (PaymentStatusId) REFERENCES PaymentStatus(StatusId)
);
GO

-- =============================================
-- TABLA: Payments (Pagos)
-- =============================================
CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    QuotaId INT NOT NULL,
    StudentId INT NOT NULL,
    Monto DECIMAL(10,2) NOT NULL,
    FechaPago DATETIME NOT NULL DEFAULT GETDATE(),
    PaymentTypeId INT NOT NULL,
    BankId INT NULL,
    NumeroOperacion NVARCHAR(50) NULL,
    Observaciones NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (QuotaId) REFERENCES Quotas(QuotaId),
    FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
    FOREIGN KEY (PaymentTypeId) REFERENCES PaymentTypes(PaymentTypeId),
    FOREIGN KEY (BankId) REFERENCES Banks(BankId)
);
GO

-- =============================================
-- TABLA: CursosVacacionales
-- =============================================
CREATE TABLE CursosVacacionales (
    CursoVacacionalId INT IDENTITY(1,1) PRIMARY KEY,
    NombreCurso NVARCHAR(200) NOT NULL,
    Descripcion NVARCHAR(1000) NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    Costo DECIMAL(10,2) NOT NULL,
    CapacidadMaxima INT NOT NULL,
    CuposDisponibles INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- =============================================
-- TABLA: InscripcionesCursosVacacionales
-- =============================================
CREATE TABLE InscripcionesCursosVacacionales (
    InscripcionId INT IDENTITY(1,1) PRIMARY KEY,
    CursoVacacionalId INT NOT NULL,
    StudentId INT NOT NULL,
    LegalGuardianId INT NOT NULL,
    FechaInscripcion DATETIME NOT NULL DEFAULT GETDATE(),
    Monto DECIMAL(10,2) NOT NULL,
    Estado NVARCHAR(50) NOT NULL DEFAULT 'Activa',
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (CursoVacacionalId) REFERENCES CursosVacacionales(CursoVacacionalId),
    FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
    FOREIGN KEY (LegalGuardianId) REFERENCES LegalGuardians(LegalGuardianId)
);
GO

-- =============================================
-- TABLA: QuotasCursosVacacionales
-- =============================================
CREATE TABLE QuotasCursosVacacionales (
    QuotaVacacionalId INT IDENTITY(1,1) PRIMARY KEY,
    InscripcionId INT NOT NULL,
    StudentId INT NOT NULL,
    Mes NVARCHAR(20) NOT NULL,
    Anio INT NOT NULL,
    Monto DECIMAL(10,2) NOT NULL,
    FechaVencimiento DATE NOT NULL,
    PaymentStatusId INT NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (InscripcionId) REFERENCES InscripcionesCursosVacacionales(InscripcionId),
    FOREIGN KEY (StudentId) REFERENCES Students(StudentId),
    FOREIGN KEY (PaymentStatusId) REFERENCES PaymentStatus(StatusId)
);
GO

-- =============================================
-- TABLA: ConfiguracionCosto
-- =============================================
CREATE TABLE ConfiguracionCosto (
    ConfiguracionId INT IDENTITY(1,1) PRIMARY KEY,
    GradoSeccionId INT NULL,
    Anio INT NOT NULL,
    MontoMatricula DECIMAL(10,2) NOT NULL,
    MontoPension DECIMAL(10,2) NOT NULL,
    FechaVigencia DATE NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (GradoSeccionId) REFERENCES GradoSecciones(GradoSeccionId)
);
GO

-- =============================================
-- TABLA: CarritoCompras
-- =============================================
CREATE TABLE CarritoCompras (
    CarritoId INT IDENTITY(1,1) PRIMARY KEY,
    LegalGuardianId INT NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
    MontoTotal DECIMAL(10,2) NOT NULL DEFAULT 0,
    Estado NVARCHAR(50) NOT NULL DEFAULT 'Activo',
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (LegalGuardianId) REFERENCES LegalGuardians(LegalGuardianId)
);
GO

-- =============================================
-- TABLA: DetallesCarrito
-- =============================================
CREATE TABLE DetallesCarrito (
    DetalleId INT IDENTITY(1,1) PRIMARY KEY,
    CarritoId INT NOT NULL,
    QuotaId INT NULL,
    QuotaVacacionalId INT NULL,
    Concepto NVARCHAR(500) NOT NULL,
    Monto DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (CarritoId) REFERENCES CarritoCompras(CarritoId),
    FOREIGN KEY (QuotaId) REFERENCES Quotas(QuotaId),
    FOREIGN KEY (QuotaVacacionalId) REFERENCES QuotasCursosVacacionales(QuotaVacacionalId)
);
GO

-- =============================================
-- TABLA: TransaccionesPago
-- =============================================
CREATE TABLE TransaccionesPago (
    TransaccionId INT IDENTITY(1,1) PRIMARY KEY,
    CarritoId INT NOT NULL,
    MontoTotal DECIMAL(10,2) NOT NULL,
    FechaTransaccion DATETIME NOT NULL DEFAULT GETDATE(),
    PaymentTypeId INT NOT NULL,
    BankId INT NULL,
    NumeroTarjeta NVARCHAR(4) NULL,
    CodigoAutorizacion NVARCHAR(50) NULL,
    Estado NVARCHAR(50) NOT NULL DEFAULT 'Completada',
    FOREIGN KEY (CarritoId) REFERENCES CarritoCompras(CarritoId),
    FOREIGN KEY (PaymentTypeId) REFERENCES PaymentTypes(PaymentTypeId),
    FOREIGN KEY (BankId) REFERENCES Banks(BankId)
);
GO

-- =============================================
-- INSERTAR DATOS INICIALES
-- =============================================

-- Estados de Pago
INSERT INTO PaymentStatus (StatusName) VALUES 
('Pendiente'),
('Pagado'),
('Vencido');
GO

-- Tipos de Pago
INSERT INTO PaymentTypes (TypeName, IsActive) VALUES 
('Efectivo', 1),
('Transferencia', 1),
('Tarjeta de Crédito', 1),
('Tarjeta de Débito', 1);
GO

-- Bancos
INSERT INTO Banks (BankName, IsActive) VALUES 
('Banco de Crédito del Perú (BCP)', 1),
('BBVA Continental', 1),
('Scotiabank', 1),
('Interbank', 1),
('Banco de la Nación', 1),
('Banco Pichincha', 1),
('Banbif', 1),
('Banco Falabella', 1);
GO

-- Tipos de Curso
INSERT INTO TipoCurso (Nombre, Descripcion) VALUES 
('Curso de Verano', 'Cursos recreativos y educativos durante el verano'),
('Curso de Invierno', 'Cursos de reforzamiento durante el invierno'),
('Talleres Especiales', 'Talleres de arte, música, deportes, etc.');
GO

-- Usuarios del sistema
INSERT INTO Users (Username, PasswordHash, Role, IsActive) VALUES 
('admin', 'admin123', 'Administrador', 1),
('secretaria', 'secre123', 'Secretaria', 1);
GO

-- =============================================
-- DATOS DE PRUEBA
-- =============================================

-- Apoderados
INSERT INTO LegalGuardians (DNI, Nombre, Apellido, Direccion, Telefono, Email, IsActive) VALUES 
('12345678', 'Carlos', 'García López', 'Av. Arequipa 1234, San Isidro', '987654321', 'carlos.garcia@email.com', 1),
('23456789', 'María', 'Rodríguez Pérez', 'Jr. Lima 567, Miraflores', '987654322', 'maria.rodriguez@email.com', 1),
('34567890', 'José', 'Fernández Torres', 'Av. Javier Prado 890, San Borja', '987654323', 'jose.fernandez@email.com', 1),
('45678901', 'Ana', 'Martínez Silva', 'Calle Los Olivos 234, Surco', '987654324', 'ana.martinez@email.com', 1);
GO

-- Usuarios Apoderados
INSERT INTO Users (Username, PasswordHash, Role, IsActive) VALUES 
('Carlos12345678', 'apod123', 'Apoderado', 1),
('María23456789', 'apod123', 'Apoderado', 1),
('José34567890', 'apod123', 'Apoderado', 1),
('Ana45678901', 'apod123', 'Apoderado', 1);
GO

-- Estudiantes
INSERT INTO Students (DNI, Nombre, Apellido, FechaNacimiento, Direccion, Telefono, Email, LegalGuardianId, IsActive) VALUES 
('78901234', 'Diego', 'García Ramírez', '2015-03-15', 'Av. Arequipa 1234, San Isidro', NULL, NULL, 1, 1),
('78901235', 'Sofía', 'García Ramírez', '2017-07-22', 'Av. Arequipa 1234, San Isidro', NULL, NULL, 1, 1),
('89012345', 'Lucas', 'Rodríguez Castro', '2016-05-10', 'Jr. Lima 567, Miraflores', NULL, NULL, 2, 1),
('90123456', 'Valentina', 'Fernández Quispe', '2014-11-30', 'Av. Javier Prado 890, San Borja', NULL, NULL, 3, 1),
('01234567', 'Mateo', 'Martínez Huamán', '2015-08-18', 'Calle Los Olivos 234, Surco', NULL, NULL, 4, 1);
GO

-- Docentes
INSERT INTO Docentes (DNI, Nombre, Apellido, Especialidad, Telefono, Email, IsActive) VALUES 
('11111111', 'Pedro', 'Sánchez Rojas', 'Matemáticas', '999111222', 'pedro.sanchez@escuela.edu.pe', 1),
('22222222', 'Laura', 'López Vega', 'Comunicación', '999111223', 'laura.lopez@escuela.edu.pe', 1),
('33333333', 'Roberto', 'Díaz Mendoza', 'Ciencias', '999111224', 'roberto.diaz@escuela.edu.pe', 1),
('44444444', 'Carmen', 'Torres Flores', 'Inglés', '999111225', 'carmen.torres@escuela.edu.pe', 1);
GO

-- Grados y Secciones
INSERT INTO GradoSecciones (Grado, Seccion, AnioEscolar, Capacidad, IsActive) VALUES 
('1° Primaria', 'A', 2025, 30, 1),
('1° Primaria', 'B', 2025, 30, 1),
('2° Primaria', 'A', 2025, 30, 1),
('3° Primaria', 'A', 2025, 30, 1),
('4° Primaria', 'A', 2025, 30, 1),
('5° Primaria', 'A', 2025, 30, 1),
('1° Secundaria', 'B', 2025, 30, 1),
('2° Secundaria', 'A', 2025, 30, 1),
('3° Secundaria', 'A', 2025, 30, 1),
('4° Secundaria', 'A', 2025, 30, 1),
('5° Secundaria', 'A', 2025, 30, 1);
GO

-- Horarios (SIN DocenteId)
INSERT INTO Horarios (GradoSeccionId, Curso, DiaSemana, HoraInicio, HoraFin, IsActive) VALUES 
(1, 'Matemáticas', 'Lunes', '08:00', '09:30', 1),
(1, 'Comunicación', 'Lunes', '09:45', '11:15', 1),
(1, 'Ciencias', 'Martes', '08:00', '09:30', 1),
(1, 'Inglés', 'Martes', '09:45', '11:15', 1),
(2, 'Matemáticas', 'Lunes', '08:00', '09:30', 1),
(2, 'Comunicación', 'Lunes', '09:45', '11:15', 1);
GO

-- Asignación de Docentes
INSERT INTO AsignacionDocente (DocenteId, HorarioId, IsActive) VALUES 
(1, 1, 1), (2, 2, 1), (3, 3, 1), (4, 4, 1), (1, 5, 1), (2, 6, 1);
GO

-- Configuración de Costos
INSERT INTO ConfiguracionCosto (GradoSeccionId, Anio, MontoMatricula, MontoPension, IsActive) VALUES 
(1, 2025, 350.00, 300.00, 1),
(2, 2025, 350.00, 300.00, 1),
(3, 2025, 380.00, 300.00, 1),
(4, 2025, 400.00, 300.00, 1),
(5, 2025, 420.00, 300.00, 1),
(6, 2025, 450.00, 300.00, 1);
GO

-- Matrículas
INSERT INTO Matriculas (StudentId, LegalGuardianId, GradoSeccionId, AnioEscolar, FechaMatricula, MontoMatricula, Estado, IsActive) VALUES 
(1, 1, 3, 2025, '2025-03-01', 380.00, 'Activa', 1),
(2, 1, 1, 2025, '2025-03-01', 350.00, 'Activa', 1),
(3, 2, 2, 2025, '2025-03-01', 350.00, 'Activa', 1),
(4, 3, 5, 2025, '2025-03-01', 420.00, 'Activa', 1),
(5, 4, 3, 2025, '2025-03-01', 380.00, 'Activa', 1);
GO

-- Generar cuotas automáticamente
DECLARE @MatriculaId INT, @StudentId INT, @Monto DECIMAL(10,2);
DECLARE @Meses TABLE (Mes NVARCHAR(20), NumMes INT);

INSERT INTO @Meses VALUES 
('Marzo', 3), ('Abril', 4), ('Mayo', 5), ('Junio', 6), 
('Julio', 7), ('Agosto', 8), ('Septiembre', 9), 
('Octubre', 10), ('Noviembre', 11), ('Diciembre', 12);

DECLARE cuotas_cursor CURSOR FOR 
SELECT m.MatriculaId, m.StudentId, c.MontoPension
FROM Matriculas m
INNER JOIN ConfiguracionCosto c ON m.GradoSeccionId = c.GradoSeccionId AND m.AnioEscolar = c.Anio
WHERE m.IsActive = 1;

OPEN cuotas_cursor;
FETCH NEXT FROM cuotas_cursor INTO @MatriculaId, @StudentId, @Monto;

WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO Quotas (MatriculaId, StudentId, Mes, Anio, Monto, FechaVencimiento, PaymentStatusId, FechaCreacion, IsActive)
    SELECT 
        @MatriculaId,
        @StudentId,
        Mes,
        2025,
        @Monto,
        DATEFROMPARTS(2025, NumMes, 10),
        CASE 
            WHEN NumMes <= 4 THEN 2
            ELSE 1
        END,
        GETDATE(),
        1
    FROM @Meses;
    
    FETCH NEXT FROM cuotas_cursor INTO @MatriculaId, @StudentId, @Monto;
END;

CLOSE cuotas_cursor;
DEALLOCATE cuotas_cursor;
GO

-- Generar pagos para cuotas pagadas
INSERT INTO Payments (QuotaId, StudentId, Monto, FechaPago, PaymentTypeId, BankId, NumeroOperacion, Observaciones, IsActive)
SELECT 
    q.QuotaId,
    q.StudentId,
    q.Monto,
    DATEADD(day, -3, q.FechaVencimiento),
    2,
    1,
    'OP-2025' + RIGHT('00000' + CAST(q.QuotaId AS NVARCHAR), 5),
    'Pago realizado a tiempo',
    1
FROM Quotas q
WHERE q.PaymentStatusId = 2 AND q.IsActive = 1;
GO

-- Cursos Vacacionales
INSERT INTO CursosVacacionales (NombreCurso, Descripcion, FechaInicio, FechaFin, Costo, CapacidadMaxima, CuposDisponibles, IsActive) VALUES 
('Curso de Verano 2025 - Natación', 'Clases de natación para niños de 6 a 12 años', '2025-01-15', '2025-02-28', 600.00, 20, 17, 1),
('Curso de Verano 2025 - Arte y Pintura', 'Taller de arte y pintura creativa', '2025-01-15', '2025-02-28', 450.00, 15, 14, 1),
('Curso de Verano 2025 - Fútbol', 'Escuela de fútbol infantil', '2025-01-15', '2025-02-28', 500.00, 25, 24, 1);
GO

-- Inscripciones a cursos vacacionales
INSERT INTO InscripcionesCursosVacacionales (CursoVacacionalId, StudentId, LegalGuardianId, FechaInscripcion, Monto, Estado, IsActive) VALUES 
(1, 1, 1, '2025-01-10', 600.00, 'Activa', 1),
(2, 2, 1, '2025-01-10', 450.00, 'Activa', 1);
GO

-- Generar cuotas de cursos vacacionales
INSERT INTO QuotasCursosVacacionales (InscripcionId, StudentId, Mes, Anio, Monto, FechaVencimiento, PaymentStatusId, FechaCreacion, IsActive)
SELECT 
    i.InscripcionId,
    i.StudentId,
    CASE n.Numero
        WHEN 1 THEN 'Enero'
        WHEN 2 THEN 'Febrero'
        WHEN 3 THEN 'Marzo'
    END,
    2025,
    i.Monto / 3,
    CASE n.Numero
        WHEN 1 THEN '2025-01-20'
        WHEN 2 THEN '2025-02-10'
        WHEN 3 THEN '2025-02-25'
    END,
    CASE 
        WHEN n.Numero = 1 THEN 2
        ELSE 1
    END,
    GETDATE(),
    1
FROM InscripcionesCursosVacacionales i
CROSS JOIN (SELECT 1 AS Numero UNION SELECT 2 UNION SELECT 3) n
WHERE i.IsActive = 1;
GO

PRINT '=================================================';
PRINT 'BASE DE DATOS CREADA EXITOSAMENTE';
PRINT '=================================================';
PRINT 'CAMBIOS IMPLEMENTADOS:';
PRINT '  ✓ IsActive agregado a: PaymentTypes, LegalGuardians, Docentes';
PRINT '  ✓ Horarios SIN DocenteId (solo tiene Curso)';
PRINT '  ✓ QuotasCursosVacacionales con Mes/Anio en lugar de NumeroCuota';
PRINT '=================================================';
PRINT 'Usuarios del sistema:';
PRINT '  - Admin: admin / admin123';
PRINT '  - Secretaria: secretaria / secre123';
PRINT '=================================================';
GO
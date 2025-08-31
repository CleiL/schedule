/* =========================================================================
   SCHEMA SQL SERVER — SCHEDULE
   - Idempotente: só cria se não existir
   - Regras de negócio no banco:
       1) Um profissional só pode atender 1 consulta por horário
          => UNIQUE (HealthcareId, StartAt)
       2) Um paciente só pode ter 1 consulta por profissional por dia
          => UNIQUE (PatientId, HealthcareId, [Date])
       3) Consulta em grade de 30 minutos
          => CHECK em StartAt (sem segundos/ms e minuto múltiplo de 30)
   ========================================================================== */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

/* -----------------------------
   Patients
   ----------------------------- */
IF OBJECT_ID(N'dbo.Patients','U') IS NULL
BEGIN
    CREATE TABLE dbo.Patients
    (
        PatientId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Patients PRIMARY KEY,
        [Name]   NVARCHAR(200)     NOT NULL,
        Email    NVARCHAR(256)     NULL,
        CPF      VARCHAR(14)       NOT NULL
    );

    /* unicidades */
    CREATE UNIQUE INDEX UX_Patients_CPF
        ON dbo.Patients (CPF);

    CREATE UNIQUE INDEX UX_Patients_Email
        ON dbo.Patients (Email)
        WHERE Email IS NOT NULL;
END;

/* -----------------------------
   Healthcare Professionals
   ----------------------------- */
IF OBJECT_ID(N'dbo.Healthcare','U') IS NULL
BEGIN
    CREATE TABLE dbo.Healthcare
    (
        HealthcareId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Healthcare PRIMARY KEY,
        [Name]       NVARCHAR(200)    NOT NULL,
        Email        NVARCHAR(256)    NULL,
        CRM          NVARCHAR(50)     NOT NULL,
        Speciality   NVARCHAR(120)    NOT NULL   -- <== igual à classe (Speciality)
    );

    CREATE UNIQUE INDEX UX_Healthcare_CRM
        ON dbo.Healthcare (CRM);

    CREATE UNIQUE INDEX UX_Healthcare_Email
        ON dbo.Healthcare (Email)
        WHERE Email IS NOT NULL;
END;

/* -----------------------------
   Users
   ----------------------------- */
IF OBJECT_ID(N'dbo.Users','U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId       UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Users PRIMARY KEY,
        Email        NVARCHAR(256)    NOT NULL,
        PasswordHash NVARCHAR(200)    NOT NULL,
        [Role]       NVARCHAR(50)     NOT NULL,
        PatientId    UNIQUEIDENTIFIER NULL,
        HealthcareId UNIQUEIDENTIFIER NULL,

        CONSTRAINT FK_Users_Patient
            FOREIGN KEY (PatientId)
            REFERENCES dbo.Patients (PatientId),

        CONSTRAINT FK_Users_Healthcare
            FOREIGN KEY (HealthcareId)
            REFERENCES dbo.Healthcare (HealthcareId)
    );

    CREATE UNIQUE INDEX UX_Users_Email
        ON dbo.Users (Email);
END;

/* -----------------------------
   Appointments
   ----------------------------- */
IF OBJECT_ID(N'dbo.Appointments','U') IS NULL
BEGIN
    CREATE TABLE dbo.Appointments
    (
        AppointmentId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Appointments PRIMARY KEY,
        StartAt       DATETIME2(0)     NOT NULL,
        [Date]        AS CAST(StartAt AS date) PERSISTED,
        PatientId     UNIQUEIDENTIFIER NOT NULL,
        HealthcareId  UNIQUEIDENTIFIER NOT NULL,

        CONSTRAINT FK_Appointments_Patient
            FOREIGN KEY (PatientId)
            REFERENCES dbo.Patients (PatientId),

        CONSTRAINT FK_Appointments_Healthcare
            FOREIGN KEY (HealthcareId)
            REFERENCES dbo.Healthcare (HealthcareId),

        /* grade de 30 minutos: sem seg/ms e minuto múltiplo de 30 */
        CONSTRAINT CK_Appointments_ThirtyMinuteGrid
            CHECK (
                DATEPART(SECOND, StartAt) = 0
                AND DATEPART(MILLISECOND, StartAt) = 0
                AND (DATEPART(MINUTE, StartAt) % 30) = 0
            )
    );

    /* Regra 1: profissional só 1 por horário */
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
         WHERE name = N'UX_Appointments_Healthcare_StartAt'
           AND object_id = OBJECT_ID(N'dbo.Appointments')
    )
    CREATE UNIQUE INDEX UX_Appointments_Healthcare_StartAt
        ON dbo.Appointments (HealthcareId, StartAt);

    /* Regra 2: paciente só 1 por dia por profissional */
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
         WHERE name = N'UX_Appointments_Patient_Healthcare_Date'
           AND object_id = OBJECT_ID(N'dbo.Appointments')
    )
    CREATE UNIQUE INDEX UX_Appointments_Patient_Healthcare_Date
        ON dbo.Appointments (PatientId, HealthcareId, [Date]);

    /* Apoio às buscas comuns */
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
         WHERE name = N'IX_Appointments_Healthcare_Date'
           AND object_id = OBJECT_ID(N'dbo.Appointments')
    )
    CREATE INDEX IX_Appointments_Healthcare_Date
        ON dbo.Appointments (HealthcareId, [Date], StartAt DESC);

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
         WHERE name = N'IX_Appointments_Patient_Date'
           AND object_id = OBJECT_ID(N'dbo.Appointments')
    )
    CREATE INDEX IX_Appointments_Patient_Date
        ON dbo.Appointments (PatientId, [Date], StartAt DESC);
END;

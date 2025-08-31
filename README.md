# schedule
Aplicação para gestão de agendamentos de consultas em uma clínica.


Schedule — Execução Local (sem Docker)

Aplicação para gestão de agendamentos de uma clínica, composta por API .NET 8 e Frontend Angular.

Sumário

Pré-requisitos

Banco de Dados

API (.NET 8)

Frontend (Angular)

Comandos úteis

Estrutura do projeto

Troubleshooting

Pré-requisitos

.NET SDK 8.0
https://dotnet.microsoft.com/download

Node.js 18/20 LTS + npm
https://nodejs.org

Angular CLI (global):

npm i -g @angular/cli


SQL Server (Developer/Express) com alguma ferramenta para rodar SQL

SSMS ou

sqlcmd (vem com SQL Server/SSMS)

Se for usar login sa, ative Autenticação SQL (Mixed Mode) no SQL Server.

Banco de Dados

Criar o banco (sugerido: ScheduleDb) e aplicar o schema.

Via SSMS (GUI)
Abra e execute o arquivo:

Schedule.Infra/Data/Scripts/schema.sql


Via sqlcmd (terminal)

:: cria o banco se ainda não existir
sqlcmd -S . -d master -Q "IF DB_ID('ScheduleDb') IS NULL CREATE DATABASE [ScheduleDb]"

:: aplica o schema
sqlcmd -S . -d ScheduleDb -i ".\Schedule.Infra\Data\Scripts\schema.sql"


Usuário Admin

admin@email.com
Mudar@123

O script schema.sql já insere um usuário com Role = Admin.

Caso queira outro e-mail/senha, edite a linha do INSERT no schema.sql e execute novamente.

Importante: troque a senha padrão assim que possível.

API (.NET 8)

Connection string em Schedule/Schedule.Server/appsettings.Development.json:

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ScheduleDb;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True;Encrypt=False"
  },
  "JWTOptions": {
    "Issuer": "cinovasyn.com.br",
    "Audience": "cinovasyn.com.br",
    "SecretKey": "troque-por-um-segredo-bem-grande-32+chars",
    "ExpiresMinutes": 120
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  }
}


Usando Windows Authentication:

Server=localhost;Database=ScheduleDb;Trusted_Connection=True;TrustServerCertificate=True


CORS (para permitir o Angular em http://localhost:4200). No Program.cs:

builder.Services.AddCors(o =>
{
    o.AddPolicy("dev", p => p
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors("dev");


Restaurar, compilar e rodar:

dotnet restore
dotnet build
dotnet run --project Schedule/Schedule.Server


A API exibirá as URLs no console (ex.: http://localhost:5188).

Swagger: http://localhost:<porta-da-api>/swagger

Frontend (Angular)

Instalar dependências:

cd Schedule/schedule.client
npm ci   # ou: npm install


Apontar o endpoint da API em:

Schedule/schedule.client/src/environments/environment.ts

export const environment = {
  production: false,
  apiUrl: 'http://localhost:5188' // ajuste para a porta REAL da sua API
};


Se optar por HTTPS, use https://localhost:<porta>, garantindo que o certificado de dev esteja confiável.

Subir o Angular:

npm start


Acesse: http://localhost:4200

Comandos úteis
# Rodar somente API
dotnet run --project Schedule/Schedule.Server

# Rodar somente o Front
cd Schedule/schedule.client
npm start

# Build de produção do front
npm run build -- --configuration=production

Estrutura do projeto
schedule/
├─ Schedule/                     # Solução Web
│  ├─ Schedule.Server/           # API .NET 8 (Swagger, Controllers, Auth)
│  └─ schedule.client/           # Frontend Angular
├─ Schedule.Application/         # Casos de uso / Services
├─ Schedule.Core/                # Domínio / Entidades
├─ Schedule.Infra/               # Repositórios, Data Access
│  └─ Data/Scripts/schema.sql    # Script do banco
└─ Schedule.Test/                # (se aplicável) testes

Troubleshooting

1) “Welcome to nginx!” ao abrir o app
Você provavelmente subiu via Docker antes e está acessando a porta do Nginx.
Sem Docker, o front roda em http://localhost:4200
 e a API na porta mostrada pelo dotnet run.

2) 401/403 ou CORS

Confirme environment.ts com o apiUrl correto.

Garanta o UseCors("dev") e o origin http://localhost:4200.

Verifique se o login está retornando JWT e se o front envia Authorization: Bearer <token>.

3) Falha para conectar no SQL

Verifique a connection string, credenciais (sa/senha) e se o SQL está aceitando o modo de autenticação escolhido.

Se usar Windows Auth, ajuste Trusted_Connection=True.

4) Porta errada

O Angular deve apontar para a porta exata que a API informa ao iniciar (ex.: http://localhost:5188).

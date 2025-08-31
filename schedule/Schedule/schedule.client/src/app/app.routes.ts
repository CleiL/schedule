import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { PROFILE_SERVICE, PROFILE_TYPE } from './contracts/profile.tokens';
import { PatientService } from './services/patient.service';
import { MedicoService } from './services/medico.service';


export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./pages/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./pages/register-user.component').then(m => m.RegisterUserComponent) },
  { path: 'register-medico', loadComponent: () => import('./pages/register-medico.component').then(m => m.RegisterMedicoComponent) },
  {
    path: 'home', canActivate: [authGuard], loadComponent: () => import('./pages/layout.component').then(m => m.LayoutComponent),
    children: [
      { path: 'agenda', loadComponent: () => import('./pages/agenda.component').then(m => m.AgendaComponent) },
      { path: 'consultas', loadComponent: () => import('./pages/consulta.component').then(m => m.ConsultaComponent) },
    ]
  },
];

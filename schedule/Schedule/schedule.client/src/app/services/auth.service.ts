import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { Observable, tap } from 'rxjs';
import { LoginResponse } from '../interfaces/login';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  constructor(
    private http: HttpClient,
  ) { }

  private apiUrl = environment.apiUrls[0];

  login(data: { email: string, password: string }): Observable<any> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/Auth/login`, data).pipe(
      tap((res: any) => {
        localStorage.setItem('access_token', res.token);
        if (res.nome) localStorage.setItem('nome', res.nome);
      })
    );
  }

  logout() {
    localStorage.removeItem('access_token');
    localStorage.removeItem('nome');
  }

  register(data: { name: string, email: string, cpf: string, password: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/register/patient`, data);
  }

  registerMedico(data: { name: string, email: string, crm: string, password: string, speciality: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/register/healthcare`, data);
  }

  isAuthenticated(): boolean {
    const t = this.token;
    if (!t) return false;
    const exp = this.jwtExp(t);
    return exp ? Date.now() / 1000 < exp : true; 
  }

  get token(): string | null {
    return localStorage.getItem('access_token');
  }

  private normalizeRole(v?: string | null): string {
    return (v ?? '')
      .trim()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '') // remove acentos
      .toLowerCase();
  }

  /** Role normalizada: 'paciente' | 'medico' | 'healthcare' | 'admin' */
  get role(): string {
    const r = this.jwtRole(this.token);
    return this.normalizeRole(r);
  }

  get isPaciente(): boolean {
    const r = this.role;
    return r === 'paciente' || r === 'patient';
  }

  get isMedico(): boolean {
    const r = this.role;
    return r === 'medico' || r === 'healthcare';  // <- aqui estava o bug
  }

  get isAdmin(): boolean {
    return this.role === 'admin';
  }

  get userId(): string | null {
    const payload = this.decodePayload<any>(this.token);
    return payload?.sub ?? null;
  }

  // ====== Helpers de JWT ======

  private decodePayload<T = any>(token: string | null): T | null {
    if (!token) return null;
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    try {
      // corrige base64url
      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const json = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(json) as T;
    } catch {
      return null;
    }
  }

  /** Tenta extrair a role das claims comuns (role, roles, claim URI da Microsoft) */
  private jwtRole(token: string | null): string | null {
    const p = this.decodePayload<any>(token);
    if (!p) return null;
    return (
      p.role ??
      p.roles ??
      p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
      null
    );
  }

  /** Epoch seconds do exp, se houver */
  private jwtExp(token: string | null): number | null {
    const p = this.decodePayload<any>(token);
    return p?.exp ?? null;
  }
}

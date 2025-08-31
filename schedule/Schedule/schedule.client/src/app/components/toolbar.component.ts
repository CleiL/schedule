import { CommonModule } from "@angular/common";
import { Component, inject } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { MatToolbarModule } from "@angular/material/toolbar";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { AuthService } from "../services/auth.service";

@Component({
  selector: "app-toolbar",
  standalone: true,
  imports: [
    // Angular core modules
    CommonModule,
    RouterModule,

    // Angular Material components can be imported here if needed
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
  ],
  providers: [],
  template: `
        <mat-toolbar>
      <span>Agendor</span>
      <span class="toolbar-spacer"></span>

      <div class="menu-spacer">
        <!-- Paciente: só Agenda -->
        <button *ngIf="auth.isPaciente || auth.isAdmin" mat-mini-fab matTooltip="Agenda" [routerLink]="['/home/agenda']">
          <mat-icon class="material-symbols-outlined">view_agenda</mat-icon>
        </button>

        <!-- Médico: só Consultas -->
        <button *ngIf="auth.isMedico || auth.isAdmin" mat-mini-fab matTooltip="Consultas" [routerLink]="['/home/consultas']">
          <!-- 'patient_list' pode não existir em todos os sets; use um ícone garantido -->
          <mat-icon class="material-symbols-outlined">assignment</mat-icon>
        </button>
      </div>

      <span class="toolbar-spacer"></span>
      <div class="menu-spacer">

        <button mat-mini-fab matTooltip="Sair" (click)="logout()">
          <mat-icon>logout</mat-icon>
        </button>
      </div>
    </mat-toolbar>
    `,
  styles: [`
        :host {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            width: 100%;
            height: 100%;
        }

        .toolbar-spacer {
            flex: 1 1 auto;
        }
        .menu-spacer  {
            display: flex;
            gap: 0.5rem;
        }
    `]
})

export class ToolbarComponent {
  readonly auth = inject(AuthService);
  readonly router = inject(Router);

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}

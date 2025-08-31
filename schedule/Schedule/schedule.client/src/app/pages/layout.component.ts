import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { RouterModule } from "@angular/router";
import { ToolbarComponent } from "../components/toolbar.component";

@Component({
  selector: "app-layout",
  standalone: true,
  imports: [
    // Angular core modules
    CommonModule,
    RouterModule,

    ToolbarComponent,
  ],
  providers: [],
  template: `
        <main>
            <app-toolbar></app-toolbar>
            <ng-content>
                <router-outlet></router-outlet>
            </ng-content>
        </main>
    `,
  styles: [`
        :host {

        }
    `]
})

export class LayoutComponent {
}

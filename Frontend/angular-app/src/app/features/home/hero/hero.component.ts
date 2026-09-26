import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';

@Component({
  selector: 'app-hero',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './hero.component.html',
  styleUrl: './hero.component.scss'
})
export class HeroComponent {
  selectedDomaine = 'Informatique & Digital Banking';
  selectedType = 'Stage PFE (4 à 6 mois)';

  constructor(private router: Router) {}

  postuler(): void {
    this.router.navigate(['/auth/sign-up']);
  }
}

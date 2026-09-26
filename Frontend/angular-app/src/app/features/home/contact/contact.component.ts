import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.scss'
})
export class ContactComponent {
  sent = false;
  name = '';
  email = '';
  message = '';

  submit(): void {
    if (!this.name.trim() || !this.email.trim() || !this.message.trim()) {
      return;
    }
    this.sent = true;
    this.name = '';
    this.email = '';
    this.message = '';
  }
}

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface BarData {
  label: string;
  value: number;
  color?: string;
}

@Component({
  selector: 'app-bar-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './bar-chart.component.html',
  styleUrl: './bar-chart.component.scss'
})
export class BarChartComponent {
  @Input() data: BarData[] = [];
  @Input() maxValue: number | null = null;
  @Input() title = '';

  readonly barColors = ['#002B5C', '#004A8F', '#0067B3', '#0085CA', '#4DB8E8', '#8ED3F5'];

  get colors(): string[] {
    return this.data.map((d, i) => d.color || this.barColors[i % this.barColors.length]);
  }

  get normalizedMax(): number {
    if (this.maxValue && this.maxValue > 0) return this.maxValue;
    return Math.max(...this.data.map(d => d.value), 1);
  }

  get hasData(): boolean {
    return this.data.length > 0;
  }
}

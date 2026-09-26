import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface DonutSlice {
  label: string;
  value: number;
  color: string;
}

@Component({
  selector: 'app-donut-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './donut-chart.component.html',
  styleUrl: './donut-chart.component.scss'
})
export class DonutChartComponent {
  @Input() data: DonutSlice[] = [];
  @Input() title = '';

  get total(): number {
    return this.data.reduce((sum, d) => sum + d.value, 0);
  }

  get hasData(): boolean {
    return this.data.length > 0 && this.total > 0;
  }

  getSvgCoordinates(index: number): { strokeDasharray: string; strokeDashoffset: number } {
    if (!this.hasData) return { strokeDasharray: '0 100', strokeDashoffset: 0 };

    let cumulative = 0;
    for (let i = 0; i < index; i++) {
      cumulative += this.data[i].value / this.total;
    }

    const sliceSize = this.data[index].value / this.total;
    const offset = -cumulative * 100;

    return { strokeDasharray: `${sliceSize * 100} ${100 - sliceSize * 100}`, strokeDashoffset: offset };
  }
}

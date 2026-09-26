import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface PipelineStage {
  label: string;
  value: number;
}

@Component({
  selector: 'app-pipeline-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pipeline-chart.component.html',
  styleUrl: './pipeline-chart.component.scss'
})
export class PipelineChartComponent {
  @Input() data: PipelineStage[] = [];
  @Input() title = '';

  get total(): number {
    return this.data.length > 0 ? this.data[0].value : 0;
  }

  Math = Math;

  get hasData(): boolean {
    return this.data.length > 0;
  }
}

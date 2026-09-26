import os
import subprocess

html_content = """<!DOCTYPE html>
<html lang="fr">
<head>
<meta charset="UTF-8">
<title>Rapport de Stage d'Été — Abdelkader Kaddachi — STB StageConnect 2026</title>
<style>
  @page {
    size: A4;
    margin: 20mm 18mm 22mm 18mm;
    @bottom-right {
      content: counter(page);
    }
  }

  body {
    font-family: 'Segoe UI', Arial, Helvetica, sans-serif;
    font-size: 10.5pt;
    line-height: 1.6;
    color: #1e293b;
    background-color: #ffffff;
    margin: 0;
    padding: 0;
  }

  .page-break {
    page-break-after: always;
  }

  /* Cover Page */
  .cover {
    height: 94vh;
    max-height: 250mm;
    display: flex;
    flex-direction: column;
    justify-content: space-between;
    box-sizing: border-box;
    padding: 10px 10px;
    border-top: 6px solid #004b87;
    border-bottom: 6px solid #c8a14b;
    page-break-inside: avoid;
    break-inside: avoid;
  }

  .cover-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 2px solid #e2e8f0;
    padding-bottom: 15px;
  }

  .cover-header .institution {
    font-size: 14pt;
    font-weight: 800;
    color: #b91c1c;
    letter-spacing: 1px;
  }

  .cover-header .sub-institution {
    font-size: 8pt;
    color: #64748b;
    text-transform: uppercase;
    letter-spacing: 1.5px;
  }

  .cover-header .company {
    text-align: right;
  }

  .cover-header .company-name {
    font-size: 16pt;
    font-weight: 900;
    color: #004b87;
  }

  .cover-header .company-desc {
    font-size: 8.5pt;
    color: #64748b;
  }

  .cover-body {
    margin: auto 0;
    text-align: center;
  }

  .report-tag {
    display: inline-block;
    background: #e0f2fe;
    color: #0369a1;
    font-size: 10pt;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 2px;
    padding: 6px 16px;
    border-radius: 20px;
    margin-bottom: 20px;
  }

  .cover-title {
    font-size: 24pt;
    font-weight: 800;
    color: #0f172a;
    line-height: 1.25;
    margin: 0 0 12px 0;
  }

  .cover-subtitle {
    font-size: 13pt;
    font-weight: 600;
    color: #004b87;
    margin: 0 0 10px 0;
  }

  .cover-badge {
    display: inline-block;
    background: #f1f5f9;
    color: #334155;
    font-size: 9.5pt;
    font-weight: 500;
    padding: 6px 14px;
    border-radius: 6px;
    margin-top: 10px;
  }

  .cover-footer {
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    background: #f8fafc;
    padding: 18px 24px;
    border-radius: 12px;
    border: 1px solid #e2e8f0;
  }

  .cover-footer-col {
    font-size: 9pt;
  }

  .cover-footer-col h5 {
    font-size: 8pt;
    text-transform: uppercase;
    letter-spacing: 1px;
    color: #64748b;
    margin: 0 0 6px 0;
  }

  .cover-footer-col p {
    margin: 2px 0;
    color: #1e293b;
    font-weight: 600;
  }

  /* Headings */
  h1 {
    font-size: 16pt;
    color: #004b87;
    border-bottom: 2px solid #004b87;
    padding-bottom: 5px;
    margin-top: 18px;
    margin-bottom: 12px;
    page-break-after: avoid;
    break-after: avoid;
  }

  h2 {
    font-size: 12pt;
    color: #0f172a;
    border-left: 4px solid #c8a14b;
    padding-left: 10px;
    margin-top: 14px;
    margin-bottom: 8px;
    page-break-after: avoid;
    break-after: avoid;
  }

  h3 {
    font-size: 10.5pt;
    color: #334155;
    margin-top: 12px;
    margin-bottom: 6px;
    page-break-after: avoid;
    break-after: avoid;
  }

  p {
    margin: 0 0 8px 0;
    text-align: justify;
    line-height: 1.55;
  }

  ul, ol {
    margin: 0 0 8px 0;
    padding-left: 20px;
  }

  li {
    margin-bottom: 3px;
    text-align: justify;
    line-height: 1.5;
  }

  /* Tables */
  table {
    width: 100%;
    border-collapse: collapse;
    margin: 8px 0 3px 0;
    font-size: 8pt;
    page-break-inside: auto;
    break-inside: auto;
  }

  tr {
    page-break-inside: avoid;
    break-inside: avoid;
  }

  th, td {
    padding: 5px 8px;
    border: 1px solid #cbd5e1;
    text-align: left;
    vertical-align: top;
  }

  th {
    background-color: #f1f5f9;
    color: #004b87;
    font-weight: 700;
  }

  tr:nth-child(even) td {
    background-color: #f8fafc;
  }

  .table-caption {
    font-size: 8pt;
    font-style: italic;
    color: #64748b;
    text-align: center;
    margin-top: 3px;
    margin-bottom: 10px;
    page-break-before: avoid;
    break-before: avoid;
  }

  /* Code blocks */
  pre {
    background: #0f172a;
    color: #f8fafc;
    font-family: 'Consolas', 'Courier New', monospace;
    font-size: 8pt;
    line-height: 1.38;
    padding: 8px 12px;
    border-radius: 6px;
    overflow-x: auto;
    margin: 8px 0 2px 0;
    page-break-inside: avoid;
    break-inside: avoid;
  }

  .code-caption {
    font-size: 7.5pt;
    font-style: italic;
    color: #64748b;
    text-align: center;
    margin-bottom: 8px;
    page-break-before: avoid;
    break-before: avoid;
  }

  /* Callouts */
  .callout {
    background-color: #f0fdf4;
    border-left: 4px solid #16a34a;
    padding: 8px 12px;
    border-radius: 0 6px 6px 0;
    margin: 8px 0;
    font-size: 8.5pt;
    line-height: 1.45;
  }

  .callout-info {
    background-color: #f0f9ff;
    border-left-color: #0284c7;
  }

  .callout-warning {
    background-color: #fffbeb;
    border-left-color: #d97706;
  }

  /* TOC */
  .toc-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 8px 22px;
    margin-top: 14px;
  }
  .toc-col {
    display: flex;
    flex-direction: column;
  }
  .toc-item {
    display: flex;
    justify-content: space-between;
    margin-bottom: 3.5px;
    font-size: 8pt;
    line-height: 1.35;
  }

  .toc-dots {
    flex: 1;
    border-bottom: 1px dotted #94a3b8;
    margin: 0 8px 4px 8px;
  }

  .badge {
    display: inline-block;
    padding: 2px 7px;
    border-radius: 4px;
    font-size: 7.5pt;
    font-weight: 700;
  }
  .badge-success { background: #dcfce7; color: #15803d; }
  .badge-primary { background: #e0f2fe; color: #0369a1; }
  .badge-warning { background: #fef3c7; color: #b45309; }
</style>
</head>
<body>

<!-- ==================== COVER PAGE ==================== -->
<div class="cover page-break">
  <div class="cover-header">
    <div>
      <div class="institution">ESPRIT</div>
      <div class="sub-institution">École Supérieure Privée d'Ingénierie et de Technologie<br>Honoris United Universities</div>
    </div>
    <div class="company">
      <div class="company-name">STB BANK</div>
      <div class="company-desc">Société Tunisienne de Banque<br>Direction du Capital Humain & Transformation Digitale</div>
    </div>
  </div>

  <div class="cover-body">
    <div class="report-tag">RAPPORT DE STAGE D'ÉTÉ &bull; ANNÉE UNIVERSITAIRE 2025/2026</div>
    <h1 class="cover-title">Plateforme Intelligente de Gestion des Stagiaires & Appariement par IA</h1>
    <div class="cover-subtitle">Conception & Réalisation d'une Architecture Microservices Hybride (Java / .NET 8 / Angular 18+)</div>
    <div class="cover-badge">Projet « STB StageConnect » — Société Tunisienne de Banque</div>
  </div>

  <div class="cover-footer">
    <div class="cover-footer-col">
      <h5>Réalisé par</h5>
      <p>Abdelkader KADDACHI</p>
      <p style="font-weight:400; color:#475569;">Élève Ingénieur — Software Engineering</p>
    </div>
    <div class="cover-footer-col">
      <h5>Encadrement Professionnel</h5>
      <p>M. Makrem TAIEB</p>
      <p style="font-weight:400; color:#475569;">Chef de Projet & Encadrant Technique (STB)</p>
    </div>
    <div class="cover-footer-col">
      <h5>Période du Stage</h5>
      <p>1er Juillet 2026 au 31 Août 2026</p>
      <p style="font-weight:400; color:#475569;">Siège Social STB, Tunis</p>
    </div>
  </div>
</div>

<!-- ==================== TABLE OF CONTENTS ==================== -->
<div class="page-break">
  <h1>Table des Matières</h1>
  <div class="toc-grid">
    <div class="toc-col">
      <div class="toc-item"><strong>Introduction Générale</strong><span class="toc-dots"></span><strong>3</strong></div>
      <div class="toc-item"><strong>Chapitre 1 — Contextualisation &amp; Cadre</strong><span class="toc-dots"></span><strong>4</strong></div>
      <div class="toc-item" style="padding-left:10px;">1.1 Organisme d'accueil (STB Bank)<span class="toc-dots"></span>4</div>
      <div class="toc-item" style="padding-left:10px;">1.2 Étude de l'existant &amp; limites<span class="toc-dots"></span>5</div>
      <div class="toc-item" style="padding-left:10px;">1.3 Objectifs de STB StageConnect<span class="toc-dots"></span>5</div>
      <div class="toc-item" style="padding-left:10px;">1.4 Méthodologie Agile / Releases<span class="toc-dots"></span>6</div>

      <div class="toc-item"><strong>Chapitre 2 — Architecture Système</strong><span class="toc-dots"></span><strong>7</strong></div>
      <div class="toc-item" style="padding-left:10px;">2.1 Architecture microservices hybride<span class="toc-dots"></span>7</div>
      <div class="toc-item" style="padding-left:10px;">2.2 Justification de la stack technique<span class="toc-dots"></span>8</div>
      <div class="toc-item" style="padding-left:10px;">2.3 Persistance polyglotte (MySQL/PgSQL)<span class="toc-dots"></span>9</div>
      <div class="toc-item" style="padding-left:10px;">2.4 Bus RabbitMQ / MassTransit<span class="toc-dots"></span>9</div>

      <div class="toc-item"><strong>Chapitre 3 — Analyse des Besoins</strong><span class="toc-dots"></span><strong>10</strong></div>
      <div class="toc-item" style="padding-left:10px;">3.1 Acteurs &amp; matrice des privilèges<span class="toc-dots"></span>10</div>
      <div class="toc-item" style="padding-left:10px;">3.2 Besoins fonctionnels par rôle<span class="toc-dots"></span>10</div>
      <div class="toc-item" style="padding-left:10px;">3.3 Exigences non fonctionnelles<span class="toc-dots"></span>11</div>

      <div class="toc-item"><strong>Chapitre 4 — Modélisation du Système</strong><span class="toc-dots"></span><strong>12</strong></div>
      <div class="toc-item" style="padding-left:10px;">4.1 Diagramme des cas d'utilisation<span class="toc-dots"></span>12</div>
      <div class="toc-item" style="padding-left:10px;">4.2 Modèle conceptuel JSONB<span class="toc-dots"></span>13</div>
      <div class="toc-item" style="padding-left:10px;">4.3 Diagrammes de séquences clés<span class="toc-dots"></span>14</div>

      <div class="toc-item"><strong>Chapitre 5 — Release 1 : Socle &amp; Auth</strong><span class="toc-dots"></span><strong>15</strong></div>
      <div class="toc-item" style="padding-left:10px;">5.1 API Gateway &amp; Eureka<span class="toc-dots"></span>15</div>
      <div class="toc-item" style="padding-left:10px;">5.2 Authentification JWT sécurisée<span class="toc-dots"></span>16</div>
    </div>

    <div class="toc-col">
      <div class="toc-item"><strong>Chapitre 6 — Release 2 : Métier &amp; Conventions</strong><span class="toc-dots"></span><strong>17</strong></div>
      <div class="toc-item" style="padding-left:10px;">6.1 Gestion du cycle de candidature<span class="toc-dots"></span>17</div>
      <div class="toc-item" style="padding-left:10px;">6.2 Conventions QuestPDF<span class="toc-dots"></span>18</div>

      <div class="toc-item"><strong>Chapitre 7 — Release 3 : Suivi &amp; Évaluations</strong><span class="toc-dots"></span><strong>19</strong></div>
      <div class="toc-item" style="padding-left:10px;">7.1 Journal de bord interactif<span class="toc-dots"></span>19</div>
      <div class="toc-item" style="padding-left:10px;">7.2 Évaluations &amp; attestations PDF<span class="toc-dots"></span>20</div>

      <div class="toc-item"><strong>Chapitre 8 — Release 4 : Matching par IA</strong><span class="toc-dots"></span><strong>21</strong></div>
      <div class="toc-item" style="padding-left:10px;">8.1 Problématique &amp; Objectifs IA<span class="toc-dots"></span>21</div>
      <div class="toc-item" style="padding-left:10px;">8.2 Microservice SubjectMatching<span class="toc-dots"></span>22</div>
      <div class="toc-item" style="padding-left:10px;">8.3 Parsing sémantique SharpAPI<span class="toc-dots"></span>23</div>
      <div class="toc-item" style="padding-left:10px;">8.4 Algorithme déterministe scoring<span class="toc-dots"></span>24</div>
      <div class="toc-item" style="padding-left:10px;">8.5 Acceptation &amp; « Mon Sujet »<span class="toc-dots"></span>25</div>
      <div class="toc-item" style="padding-left:10px;">8.6 Événements asynchrones RabbitMQ<span class="toc-dots"></span>26</div>

      <div class="toc-item"><strong>Chapitre 9 — Notifications &amp; Sécurité</strong><span class="toc-dots"></span><strong>27</strong></div>
      <div class="toc-item" style="padding-left:10px;">9.1 Matrice logique par rôle<span class="toc-dots"></span>27</div>
      <div class="toc-item" style="padding-left:10px;">9.2 Centre slide-over &amp; SignalR<span class="toc-dots"></span>28</div>
      <div class="toc-item" style="padding-left:10px;">9.3 Réinitialisation OWASP<span class="toc-dots"></span>29</div>

      <div class="toc-item"><strong>Chapitres 10 à 13 — Bilan &amp; Perspectives</strong><span class="toc-dots"></span><strong>30</strong></div>
      <div class="toc-item" style="padding-left:10px;">10. Tests &amp; validation qualité<span class="toc-dots"></span>30</div>
      <div class="toc-item" style="padding-left:10px;">11. Déploiement conteneurisé<span class="toc-dots"></span>31</div>
      <div class="toc-item" style="padding-left:10px;">12. Difficultés &amp; leçons apprises<span class="toc-dots"></span>32</div>
      <div class="toc-item" style="padding-left:10px;">13. Conclusion &amp; perspectives<span class="toc-dots"></span>33</div>
      <div class="toc-item"><strong>Bibliographie &amp; Annexes</strong><span class="toc-dots"></span><strong>34</strong></div>
    </div>
  </div>
</div>

<!-- ==================== INTRODUCTION GENERALE ==================== -->
<div class="page-break">
  <h1>Introduction Générale</h1>
  <p>
    Dans un contexte bancaire caractérisé par une concurrence accrue et une transformation numérique rapide, l'attraction, la gestion et la fidélisation des jeunes talents représentent un enjeu stratégique majeur. La <strong>Société Tunisienne de Banque (STB)</strong>, acteur pionnier du secteur bancaire tunisien depuis 1957, accueille chaque année plusieurs centaines d'étudiants issus des universités et écoles d'ingénieurs tunisiennes et internationales pour des stages d'initiation, de perfectionnement et des Projets de Fin d'Études (PFE).
  </p>

  <h2>Problématique</h2>
  <p>
    Historiquement, le processus de gestion des stages à la STB reposait sur des processus manuels fragmentés : réceptions de candidatures par courriels disparates, ressaisies manuelles des informations dans des tableurs, génération artisanale des conventions sous traitement de texte et tenue informelle des journaux de bord.
  </p>
  <p>
    Plus critique encore, <strong>l'affectation des thématiques de stage</strong> s'effectuait de façon arbitraire ou manuelle : les responsables RH devaient lire individuellement des centaines de CVs pour tenter d'identifier le sujet technique le plus compatible avec le profil du candidat parmi les propositions formulées par les directions de la banque (DSI, Monétique, Sécurité, Banque Digitale). Ce goulot d'étranglement entraînait :
  </p>
  <ul>
    <li>Des délais d'attente prolongés pour les étudiants et un risque élevé de perte des meilleurs profils ;</li>
    <li>Des inadéquations fréquentes entre les compétences réelles du stagiaire et la complexité du projet attribué ;</li>
    <li>Une absence totale de traçabilité en temps réel du statut d'avancement des dossiers.</li>
  </ul>

  <h2>Objectifs du Projet</h2>
  <p>
    Le projet <strong>STB StageConnect</strong> a été initié pour apporter une réponse technologique moderne et pérenne à ces défis. Les objectifs fondamentaux fixés pour ce stage étaient :
  </p>
  <ul>
    <li><strong>Digitaliser à 100% le cycle de vie du stagiaire :</strong> de l'inscription initiale jusqu'à la délivrance de l'attestation finale ;</li>
    <li><strong>Intégrer l'Intelligence Artificielle au cœur du processus RH :</strong> automatiser l'analyse sémantique des CVs (PDF/DOCX) via des modèles NLP avancés et recommander instantanément le sujet de stage optimal à l'administrateur ;</li>
    <li><strong>Garantir une expérience temps réel et collaborative :</strong> doter la plateforme d'un centre de notification unifié poussant instantanément les événements métiers (approbations, alertes, changements de thématique) vers l'ensemble des acteurs concernés (Admin RH, Stagiaire, Encadrant) ;</li>
    <li><strong>Mettre en place une architecture résiliente et sécurisée :</strong> respectant les exigences rigoureuses du secteur bancaire (microservices indépendants, chiffrement JWT, auditabilité complète, déploiement conteneurisé).</li>
  </ul>
</div>

<!-- ==================== CHAPITRE 1 ==================== -->
<div class="page-break">
  <h1>Chapitre 1 — Contextualisation & Méthodologie Agile</h1>
  <h2>1.1 Présentation de l'Organisme d'Accueil : STB Bank</h2>
  <p>
    Fondée le 3 février 1957, la <strong>Société Tunisienne de Banque (STB)</strong> est le premier établissement bancaire national créé au lendemain de l'indépendance de la Tunisie. Disposant d'un réseau dense de plus de 160 agences, la STB joue un rôle moteur dans le financement de l'économie tunisienne et conduit activement une modernisation de ses systèmes d'information. Le stage a été réalisé au sein de la Direction du Capital Humain, en étroite collaboration avec les équipes de développement logiciel et de transformation digitale.
  </p>

  <h2>1.2 Méthodologie de Travail : Scrum Adapté</h2>
  <p>
    Afin de maîtriser les risques techniques et d'intégrer rapidement les retours utilisateurs, le projet a été conduit selon la méthodologie <strong>Scrum</strong> articulée autour de 4 releases majeures d'une durée de 2 semaines chacune :
  </p>

  <table>
    <thead>
      <tr>
        <th>Release</th>
        <th>Période</th>
        <th>Périmètre Fonctionnel & Technique</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><strong>Release 1</strong></td>
        <td>Semaines 1–2</td>
        <td>Socle architectural, Spring Cloud Gateway, Auth Service JWT, Eureka, Config Server.</td>
      </tr>
      <tr>
        <td><strong>Release 2</strong></td>
        <td>Semaines 3–4</td>
        <td>Microservice Stagiaire (candidatures, upload CV), Convention.Service et moteur PDF QuestPDF.</td>
      </tr>
      <tr>
        <td><strong>Release 3</strong></td>
        <td>Semaines 5–6</td>
        <td>Évaluations mi-parcours/finale, attestations de stage, hub temps réel SignalR.</td>
      </tr>
      <tr>
        <td><strong>Release 4</strong></td>
        <td>Semaines 7–8</td>
        <td><strong>Module IA d'appariement (SubjectMatching.Service)</strong>, intégration SharpAPI, recommandation automatique, réinitialisation sécurisée par e-mail, notifications unifiées.</td>
      </tr>
    </tbody>
  </table>
  <div class="table-caption">Tableau 1.1 — Découpage temporel des releases du projet STB StageConnect</div>
</div>

<!-- ==================== CHAPITRE 2 ==================== -->
<div class="page-break">
  <h1>Chapitre 2 — Architecture Système & Choix Technologiques</h1>
  <h2>2.1 Vue d'Ensemble de l'Architecture Hybride</h2>
  <p>
    Le système STB StageConnect adopte une <strong>architecture orientée microservices hybride</strong>, tirant le meilleur parti des écosystèmes Java Spring Boot et Microsoft .NET 8 :
  </p>
  <ul>
    <li><strong>Couche d'Infrastructure Java / Spring Cloud :</strong> chargée de la passerelle réactive (Spring Cloud Gateway sur port 18080), de l'authentification centrale (Auth Service sur port 8081 avec base MySQL), et de la découverte dynamique des services (Eureka sur port 8761).</li>
    <li><strong>Couche Métier .NET 8 :</strong> composée de 5 microservices indépendants spécialisés, chacun disposant de sa propre base PostgreSQL dédiée pour une isolation complète des domaines (Database-per-Service).</li>
    <li><strong>Couche Événementielle Asynchrone :</strong> broker RabbitMQ 3.13 exploitant le framework MassTransit pour découpler la génération des documents, les alertes IA et les envois de notifications.</li>
  </ul>

  <table>
    <thead>
      <tr>
        <th>Composant</th>
        <th>Technologie</th>
        <th>Port</th>
        <th>Base de Données</th>
        <th>Rôle Principal</th>
      </tr>
    </thead>
    <tbody>
      <tr><td>API Gateway</td><td>Spring Cloud Gateway</td><td>18080</td><td>—</td><td>Point d'entrée unique, vérification JWT, CORS, rate-limit.</td></tr>
      <tr><td>Auth Service</td><td>Spring Boot 3.2</td><td>8081</td><td>MySQL 8.0</td><td>Gestion des comptes, tokens JWT & refresh tokens, password reset.</td></tr>
      <tr><td>Eureka Server</td><td>Spring Cloud Netflix</td><td>8761</td><td>—</td><td>Annuaire et découverte dynamique des services.</td></tr>
      <tr><td>Stagiaire.Service</td><td>.NET 8 / EF Core</td><td>5070</td><td>PostgreSQL 15</td><td>Candidatures, stagiaires, stockage des CVs, journal de bord, audit.</td></tr>
      <tr><td><strong>SubjectMatching</strong></td><td><strong>.NET 8 / EF Core</strong></td><td><strong>5074</strong></td><td><strong>PostgreSQL 15</strong></td><td><strong>Sujets de stage, analyse IA CVs (SharpAPI), matching & affectation.</strong></td></tr>
      <tr><td>Convention.Service</td><td>.NET 8 / QuestPDF</td><td>5071</td><td>PostgreSQL 15</td><td>Génération automatique des conventions PDF et suivi des signatures.</td></tr>
      <tr><td>Evaluation.Service</td><td>.NET 8 / QuestPDF</td><td>5072</td><td>PostgreSQL 15</td><td>Évaluations encadrant, notes /20, attestations officielles PDF.</td></tr>
      <tr><td>Notification.Service</td><td>.NET 8 / SignalR</td><td>5073</td><td>PostgreSQL 15</td><td>Hub WebSocket SignalR, emails transactionnels HTML (MailHog/SMTP).</td></tr>
      <tr><td>Frontend Client</td><td>Angular 18+ / Tailwind</td><td>4200</td><td>—</td><td>Single Page Application réactive avec Notification Center slide-over.</td></tr>
    </tbody>
  </table>
  <div class="table-caption">Tableau 2.1 — Cartographie complète des microservices de la plateforme STB StageConnect</div>
</div>

<!-- ==================== CHAPITRE 8 (RELEASE 4) ==================== -->
<div class="page-break">
  <h1>Chapitre 8 — Réalisation : Release 4 — Appariement des Sujets par IA</h1>
  <h2>8.1 Problématique de l'Affectation & Objectifs de l'IA</h2>
  <p>
    Dans les versions antérieures, l'acceptation d'une candidature par l'administrateur se limitait à affecter un département et un encadrant, sans choisir ni attribuer de thématique précise. Le stagiaire découvrait son sujet le premier jour de son arrivée, ce qui engendrait de l'impréparation, des déceptions mutuelles ou des demandes de réorientation complexes.
  </p>
  <p>
    La <strong>Release 4</strong> a introduit un changement de paradigme majeur : relier intimement l'acceptation du dossier à <strong>l'analyse automatique des compétences par IA</strong> et à l'attribution immédiate d'un sujet adapté, validé par l'administrateur et consultable par l'étudiant.
  </p>

  <h2>8.2 Microservice SubjectMatching.Service (.NET 8)</h2>
  <p>
    Développé sur le port <strong>5074</strong> avec sa base de données dédiée <code>subject_matching_db</code>, ce microservice gère le cycle de vie des sujets de stage et orchestre l'algorithme d'appariement :
  </p>

  <pre><code>// Modèle de données pour les sujets de stage (PostgreSQL JSONB)
[Table("internship_subjects")]
public class InternshipSubject {
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string TypeStage { get; set; } = "PFE";
    [Column(TypeName = "jsonb")] public List&lt;string&gt; RequiredSkills { get; set; } = new();
    [Column(TypeName = "jsonb")] public List&lt;string&gt; PreferredSkills { get; set; } = new();
    public SubjectDifficulty Difficulty { get; set; } = SubjectDifficulty.Intermediate;
    public SubjectStatus Status { get; set; } = SubjectStatus.Open;
}</code></pre>
  <div class="code-caption">Listing 8.1 — Entité InternshipSubject avec stockage des compétences sous format JSONB</div>

  <h2>8.3 Intégration de l'IA SharpAPI (Parsing NLP des CVs)</h2>
  <p>
    Pour transformer des documents non structurés (PDF, DOCX) en données exploitables, nous avons intégré l'API d'intelligence artificielle spécialisée <strong>SharpAPI</strong> (endpoint RH <code>/api/v1/hr/parse_resume</code>). L'IA extrait automatiquement :
  </p>
  <ul>
    <li>Les compétences techniques et méthodologiques du candidat (ex: <em>C#, .NET, Angular, Docker, PostgreSQL</em>) ;</li>
    <li>Le niveau d'études et le diplôme en cours préparé (ex: <em>Bac+5 Ingénieur en Informatique</em>) ;</li>
    <li>Les projets académiques et expériences pratiques déjà réalisés.</li>
  </ul>
  <div class="callout callout-info">
    <strong>Résilience &amp; Tolérance aux Pannes :</strong> Si l'API cloud externe devient inaccessible (quota, réseau), le service active automatiquement un <em>Graceful Fallback</em> vers un moteur d'extraction local mock, garantissant qu'aucune candidature n'est bloquée en production.
  </div>
</div>

<!-- ==================== CHAPITRE 8 (SUITE : SCORING & WORKFLOW) ==================== -->
<div class="page-break">
  <h2>8.4 Algorithme Déterministe de Scoring &amp; Recommandation</h2>
  <p>
    Une fois le profil IA extrait, le moteur déterministe calcule un score de compatibilité normalisé entre 0% et 100% pour chaque sujet de stage ouvert, selon la formule suivante :
  </p>
  <pre><code>Score = (SkillsRequisesMatchées / TotalSkillsRequises) * 60%
      + (SkillsSouhaitéesMatchées / TotalSkillsSouhaitées) * 25%
      + AdéquationDépartement * 15%</code></pre>

  <p>
    Le système génère également une explication intelligible pour le responsable RH :
  </p>
  <div class="callout">
    <em>« Le candidat maîtrise 5/5 compétences requises (C#, .NET, PostgreSQL, Angular, Docker). 3/4 compétences souhaitées validées. Atouts identifiés : C#, .NET, Clean Architecture. Formation académique compatible avec les objectifs du stage. »</em>
  </div>

  <h2>8.5 Parcours Utilisateur & Interface Interactive</h2>
  <ul>
    <li><strong>Côté Administrateur RH :</strong> Lors du clic sur « Accepter », la fenêtre modale affiche immédiatement le score IA, met en évidence le sujet le plus pertinent (ex. 95%), pré-remplit la sélection et impose le choix d'un encadrant habilité.</li>
    <li><strong>Côté Stagiaire (« Mon Sujet ») :</strong> Dès confirmation, le stagiaire reçoit une notification et découvre sur son espace la fiche complète de son projet. S'il souhaite une autre thématique, un formulaire lui permet de formuler une <strong>demande de changement motivée</strong>, réexaminée par la direction.</li>
  </ul>

  <h2>8.6 Événements Asynchrones RabbitMQ du Cycle Sujet</h2>
  <p>
    L'ensemble du cycle de vie du sujet est propagé par le bus RabbitMQ via 4 nouveaux contrats d'événements MassTransit :
  </p>
  <table>
    <thead>
      <tr>
        <th>Événement</th>
        <th>Producteur</th>
        <th>Consommateur(s)</th>
        <th>Action Déclenchée</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><code>SubjectProposed</code></td>
        <td>SubjectMatching.Service</td>
        <td>Notification.Service</td>
        <td>Envoi d'un email officiel au candidat avec les détails du sujet + alerte in-app.</td>
      </tr>
      <tr>
        <td><code>SubjectAccepted</code></td>
        <td>SubjectMatching.Service</td>
        <td>Notification.Service</td>
        <td>Notification de confirmation à l'élève et alerte immédiate à l'administration RH.</td>
      </tr>
      <tr>
        <td><code>SubjectChangeRequested</code></td>
        <td>SubjectMatching.Service</td>
        <td>Notification.Service</td>
        <td>Alerte haute priorité dans le panneau admin avec le motif saisi par le stagiaire.</td>
      </tr>
      <tr>
        <td><code>SubjectChangeReviewed</code></td>
        <td>SubjectMatching.Service</td>
        <td>Notification.Service</td>
        <td>Notification au stagiaire de la décision (acceptée / refusée avec commentaire).</td>
      </tr>
    </tbody>
  </table>
  <div class="table-caption">Tableau 8.1 — Événements RabbitMQ introduits dans la Release 4</div>
</div>

<!-- ==================== CHAPITRE 9 ==================== -->
<div class="page-break">
  <h1>Chapitre 9 — Notifications & Sécurité Avancée</h1>
  <h2>9.1 Matrice Logique des Notifications par Rôle</h2>
  <p>
    Pour éviter la surcharge d'informations et garantir que chaque intervenant reçoive uniquement ce qui le concerne, nous avons mis en place une politique d'isolation stricte des notifications (<code>ApplyReadScope</code>) :
  </p>

  <table>
    <thead>
      <tr>
        <th>Événement</th>
        <th>Destinataire</th>
        <th>Contenu de la Notification</th>
        <th>Canal</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>Dépôt candidature</td>
        <td><strong>Admin RH</strong></td>
        <td>Nouvelle candidature déposée par {Prénom} {Nom} ({Département})</td>
        <td>In-App + Toast SignalR</td>
      </tr>
      <tr>
        <td>Dépôt candidature</td>
        <td><strong>Stagiaire</strong></td>
        <td>Votre candidature a été enregistrée avec succès et est en cours d'examen.</td>
        <td>In-App + Toast SignalR</td>
      </tr>
      <tr>
        <td>Acceptation & Sujet</td>
        <td><strong>Stagiaire</strong></td>
        <td>Félicitations, candidature acceptée ! Sujet : "{Titre}"</td>
        <td>In-App + SignalR + Email</td>
      </tr>
      <tr>
        <td>Acceptation & Sujet</td>
        <td><strong>Encadrant</strong></td>
        <td>Nouveau stagiaire attribué : {Prénom} {Nom} vous a été assigné</td>
        <td>In-App + Toast SignalR</td>
      </tr>
      <tr>
        <td>Sujet validé</td>
        <td><strong>Admin RH</strong></td>
        <td>Le stagiaire #{Id} a accepté le sujet proposé "{Titre}"</td>
        <td>In-App + Toast SignalR</td>
      </tr>
      <tr>
        <td>Convention prête</td>
        <td><strong>Stagiaire</strong></td>
        <td>Votre convention de stage est disponible au téléchargement.</td>
        <td>In-App + SignalR + Email</td>
      </tr>
      <tr>
        <td>Évaluation soumise</td>
        <td><strong>Admin RH</strong></td>
        <td>Évaluation de {Stagiaire} (note {Note}/20) en attente de votre validation.</td>
        <td>In-App + SignalR + Email</td>
      </tr>
      <tr>
        <td>Évaluation validée</td>
        <td><strong>Stagiaire</strong></td>
        <td>Votre évaluation est validée. Note finale : {Note}/20. Attestation prête.</td>
        <td>In-App + SignalR + Email</td>
      </tr>
    </tbody>
  </table>
  <div class="table-caption">Tableau 9.1 — Matrice de distribution des notifications par profil utilisateur</div>

  <h2>9.2 Centre de Notifications Slide-Over (Angular)</h2>
  <p>
    Côté frontend, un composant réactif <code>NotificationCenterComponent</code> a été développé. Il glisse depuis la droite de l'écran, propose un filtrage <em>« Toutes »</em> ou <em>« Non lues »</em>, calcule dynamiquement le badge non lu en temps réel via SignalR et permet l'action rapide <em>« Tout marquer comme lu »</em>.
  </p>

  <h2>9.3 Réinitialisation Sécurisée de Mot de Passe</h2>
  <p>
    Remplaçant les anciens mots de passe temporaires non sécurisés, la plateforme intègre un flux cryptographique conforme aux recommandations OWASP :
  </p>
  <ol>
    <li>L'utilisateur clique sur <em>« Mot de passe oublié ? »</em> et saisit son adresse e-mail ;</li>
    <li>Le service Auth génère un jeton cryptographique aléatoire de 32 octets stocké avec une durée de validité de 15 minutes ;</li>
    <li>Un message <code>PasswordResetRequested</code> est diffusé sur RabbitMQ ;</li>
    <li>Le microservice de notification envoie un courriel contenant le lien sécurisé vers <code>/auth/reset-password?token=...</code> ;</li>
    <li>L'utilisateur saisit son nouveau mot de passe directement dans l'interface, sans transit en clair.</li>
  </ol>
</div>

<!-- ==================== CONCLUSION & ANNEXES ==================== -->
<div class="page-break">
  <h1>Chapitres 10 à 13 — Bilan, Leçons & Perspectives</h1>
  
  <h2>10. Difficultés Techniques Rencontrées & Résolutions</h2>
  <ul>
    <li><strong>Conversion d'Énumérations sous EF Core :</strong> L'entité <code>InternshipSubject</code> configurée avec <code>HasConversion&lt;string&gt;()</code> rejetait les filtres SQL lorsque les données en base contenaient l'entier brut. La normalisation des données vers <code>'Open'</code> et la tolérance logicielle ont permis de fiabiliser les requêtes.</li>
    <li><strong>Règles de Réactivité Angular Signal Writes :</strong> L'exécution d'effets réactifs déclenchant des écritures synchrones levait l'exception <code>NG0600</code>. L'application de <code>untracked()</code> et de l'option <code>{ allowSignalWrites: true }</code> a restauré la fluidité du composant de notification.</li>
    <li><strong>Scoping & Étanchéité des Données :</strong> L'administration ne devait pas être polluée par les notifications personnelles des candidats. Le filtre dynamique <code>ApplyReadScope</code> garantit désormais que chaque rôle ne consulte que son périmètre légitime.</li>
  </ul>

  <h2>11. Conclusion Générale & Bilan du Projet</h2>
  <p>
    Ce stage d'été au sein de la <strong>Société Tunisienne de Banque</strong> a permis de concevoir et de déployer une solution logicielle d'envergure industrielle. En combinant l'agilité de <strong>.NET 8</strong>, la robustesse de <strong>Spring Boot</strong>, l'intelligence artificielle de <strong>SharpAPI</strong> et l'ergonomie d'<strong>Angular</strong>, la plateforme <strong>STB StageConnect</strong> modernise radicalement la gestion des stagiaires de la STB.
  </p>
  <p>
    Le projet a été validé avec succès sur un ensemble de 5 microservices interconnectés, un bus de messages asynchrones et une couverture de tests automatisés rigoureuse.
  </p>

  <h2>Remerciements</h2>
  <p>
    J'adresse mes plus vifs remerciements à mon maître de stage, <strong>M. Makrem TAIEB</strong>, pour sa confiance, son expertise technique et ses précieux conseils tout au long de cette mission. Mes remerciements s'étendent à toute l'équipe de la STB pour leur accueil bienveillant, ainsi qu'au corps professoral de l'<strong>ESPRIT</strong> pour l'excellence de la formation d'ingénieur dispensée.
  </p>

  <div style="margin-top: 40px; text-align: center; border-top: 1px solid #cbd5e1; padding-top: 20px;">
    <p style="font-size: 9pt; color: #64748b;">
      Rapport rédigé et soutenu par <strong>Abdelkader KADDACHI</strong> — Diplôme National d'Ingénieur en Informatique — ESPRIT 2026.
    </p>
  </div>
</div>

</body>
</html>
"""

html_path = os.path.abspath("rapport_stage_complet.html")
target_name = "Rapport de Stage - Abdelkader Kaddachi - STB StageConnect.pdf"
pdf_path = os.path.abspath(target_name)

with open(html_path, "w", encoding="utf-8") as f:
    f.write(html_content)

print(f"HTML generated at: {html_path}")

edge_path = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
cmd = [
    edge_path,
    "--headless=new",
    f"--print-to-pdf={pdf_path}",
    "--no-pdf-header-footer",
    f"file:///{html_path}"
]

print("Executing Edge headless to generate publication-grade PDF...")
result = subprocess.run(cmd, capture_output=True, text=True)
print("Return code:", result.returncode)
print("Stdout:", result.stdout)
print("Stderr:", result.stderr)

if os.path.exists(html_path):
    os.remove(html_path)

if os.path.exists(pdf_path):
    size_kb = os.path.getsize(pdf_path) / 1024
    print(f"SUCCESS: Generated PDF '{pdf_path}' ({size_kb:.2f} KB)")
    # Also try to copy to the alternative name if available
    alt_name = os.path.abspath("Rapport de Stage d'Été — Abdelkader Kaddachi — STB StageConnect 2026.pdf")
    try:
        import shutil
        shutil.copy2(pdf_path, alt_name)
        print(f"Also updated: {alt_name}")
    except Exception as e:
        print(f"Note: {alt_name} is currently locked by the viewer ({e})")
else:
    print("ERROR: PDF was not generated.")

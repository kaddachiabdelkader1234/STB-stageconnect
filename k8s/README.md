# Kubernetes — Phase 7 (+ autoscaling, 2026-09-14)

## C'est quoi Kubernetes ?

Kubernetes (k8s) = Un chef d'orchestre qui gère tes conteneurs Docker.
- Si un service crash, il le redémarre automatiquement
- Si tu as besoin de plus de puissance, il ajoute des instances automatiquement
- Il gère les mises à jour sans downtime

## Comment déployer sur Kubernetes

### Prérequis
- Kubernetes installé (minikube, kind, ou Docker Desktop)
- kubectl configuré
- Images Docker buildées et pushées sur ghcr.io

### Commandes de déploiement

```bash
# 1. Créer le namespace
kubectl apply -f k8s/namespace.yaml

# 2. Créer la config et les secrets
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml

# 3. Créer les deployments, services et l'autoscaling
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/hpa.yaml

# 4. Créer l'ingress (TLS)
kubectl apply -f k8s/ingress.yaml

# Vérifier que tout tourne
kubectl get pods -n gestion-stagiaires-stb
kubectl get services -n gestion-stagiaires-stb
kubectl get hpa -n gestion-stagiaires-stb
```

## Autoscaling (HPA)

`hpa.yaml` scale le gateway (3→9) et les services les plus sollicités (2–3 → 6–8) sur CPU/mémoire.
**Le `metrics-server` est obligatoire**, sinon les HPA restent au minimum et affichent
`unable to fetch metrics` :

```bash
minikube addons enable metrics-server
# ou
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml
```

## ⚠️ Contraintes à connaître

1. **Stockage RWX obligatoire.** `stagiaire-service` et `convention-service` tournent avec
   `replicas: 3` / `2` et partagent un volume (`ReadWriteMany`) : un CV déposé sur un pod doit être
   téléchargeable depuis un autre. Cela exige une storage class RWX (NFS, CephFS, EFS, Azure
   Files…). Sur un cluster mono-nœud sans classe RWX, remettre ces deux déploiements à
   `replicas: 1`.
2. **SignalR + plusieurs réplicas.** Les groupes SignalR (`user:{userId}`) sont **en mémoire, par
   pod** : avec `replicas: 2` sur `notification-service` et plusieurs réplicas du gateway, une
   notification poussée par un pod n'atteint pas un client connecté à un autre pod. Pour scaler
   au-delà, ajouter un **backplane SignalR (Redis)** — c'est la seule brique d'infrastructure
   supplémentaire que le temps réel multi-réplicas impose.
3. **Le gateway écoute sur 18080**, pas 8080 (corrigé dans `deployment.yaml` et `service.yaml`).
4. **PostgreSQL / MySQL / RabbitMQ** ne sont pas déclarés ici : fournis par le cluster ou un
   service managé.
5. Le `rewrite-target: /` de l'ingress a été **retiré** : il réécrivait tous les chemins vers `/`
   et cassait le routage de l'API.

## Accéder aux services

```bash
# Port-forward pour accéder depuis localhost (le gateway écoute sur 18080)
kubectl port-forward svc/api-gateway 18080:18080 -n gestion-stagiaires-stb
kubectl port-forward svc/eureka-server 8761:8761 -n gestion-stagiaires-stb
kubectl port-forward svc/frontend 4200:80 -n gestion-stagiaires-stb
```

## Supprimer tout

```bash
kubectl delete namespace gestion-stagiaires-stb
```

## Structure des fichiers

- `namespace.yaml` : Isole les ressources dans un namespace dédié
- `secret.yaml` : Stocke les mots de passe (base64) — JWT, DB, RabbitMQ, SMTP
- `configmap.yaml` : Configuration non sensible (découverte, RabbitMQ, SMTP non secret)
- `deployment.yaml` : Les 9 services + les PVC de stockage, avec replicas et probes
- `service.yaml` : Expose les services dans le cluster
- `hpa.yaml` : HorizontalPodAutoscalers (gateway + services les plus sollicités)
- `ingress.yaml` : Point d'entrée HTTPS externe (API + hub SignalR + SPA)
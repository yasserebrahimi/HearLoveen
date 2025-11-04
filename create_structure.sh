#!/bin/bash

# Complete Enterprise Structure
mkdir -p {src,tests,ml-platform,mobile-app,web-dashboard,infrastructure,docs,scripts,.github}

# Backend Microservices
mkdir -p src/{ApiGateway,AudioService,AnalysisService,NotificationService,UserService,IoTService,Shared}

# Each service structure
for service in ApiGateway AudioService AnalysisService NotificationService UserService IoTService; do
    mkdir -p "src/$service"/{Api,Application,Domain,Infrastructure,Tests}
    mkdir -p "src/$service/Application"/{Commands,Queries,Behaviors,Validators,Mappings}
    mkdir -p "src/$service/Domain"/{Entities,ValueObjects,Events,Repositories}
    mkdir -p "src/$service/Infrastructure"/{Data,Services,Messaging,Caching}
done

# Shared libraries
mkdir -p src/Shared/{Domain,Application,Infrastructure}/{Abstractions,Common,Extensions}

# ML Platform
mkdir -p ml-platform/{training,inference,monitoring,notebooks,data}
mkdir -p ml-platform/training/{whisper,emotion,pronunciation}
mkdir -p ml-platform/inference/{api,grpc,batch}

# Frontend
mkdir -p web-dashboard/{src,public,tests}
mkdir -p web-dashboard/src/{components,pages,store,services,hooks,utils,types}
mkdir -p mobile-app/{src,android,ios}
mkdir -p mobile-app/src/{screens,components,services,navigation,store}

# Infrastructure
mkdir -p infrastructure/{terraform,kubernetes,helm,docker}
mkdir -p infrastructure/terraform/{modules,environments}
mkdir -p infrastructure/kubernetes/{base,overlays,monitoring}
mkdir -p infrastructure/helm/{hearloveen-api,hearloveen-ml,hearloveen-web}

# Tests
mkdir -p tests/{unit,integration,e2e,load,security}

# Documentation
mkdir -p docs/{business,technical,api,deployment,user-guides}
mkdir -p docs/business/{pitch,financial,market-research}
mkdir -p docs/technical/{architecture,design-decisions,runbooks}

# Scripts
mkdir -p scripts/{setup,deployment,testing,monitoring}

# GitHub
mkdir -p .github/{workflows,ISSUE_TEMPLATE}

echo "✅ Structure created successfully!"

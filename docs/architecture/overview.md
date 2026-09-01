# Architecture Overview

Aletheia uses a layered architecture: pure domain and numerical logic sit near the center, while data
providers, application orchestration, CLI, desktop UI, and persistence sit near the edge.

## Dependency Direction

```mermaid
flowchart TD
    CLI[Aletheia.Cli] --> APP[Aletheia.Application]
    DESK[Aletheia.Desktop] --> APP
    APP --> DATA[Aletheia.Data]
    APP --> ANALYTICS[Aletheia.Analytics]
    APP --> DYN[Aletheia.Dynamics]
    APP --> SPEC[Aletheia.Spectral]
    APP --> FC[Aletheia.Forecasting]
    APP --> SIM[Aletheia.Simulation]
    APP --> VAL[Aletheia.Validation]
    APP --> PERSIST[Aletheia.Persistence]
    DATA --> CORE[Aletheia.Core]
    ANALYTICS --> CORE
    DYN --> CORE
    SPEC --> CORE
    FC --> CORE
    SIM --> CORE
    VAL --> CORE
    PERSIST --> VAL
```

## Design Principles

- Domain types carry explicit units and identities.
- Mathematical values avoid ambiguous names when units matter.
- I/O and provider parsing are kept out of UI code.
- CLI and desktop share the same application use cases.
- Forecasts are separated from validation and from decision language.
- Prediction records are immutable; realized outcomes are separate evaluations.

## Main Runtime Flow

```mermaid
flowchart LR
    A[Provider, CSV, or sample data] --> B[FundHistory]
    B --> C[Application analysis]
    C --> D[Analytics, dynamics, spectral]
    C --> E[Forecast runs]
    E --> F[Model Arena]
    F --> G[Research report]
    G --> H[Decision signal]
```

For the detailed historical note, see [Detailed Architecture](../architecture.md).

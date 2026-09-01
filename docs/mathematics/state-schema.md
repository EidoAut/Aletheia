# State Schema Identity

## Purpose

A dynamic state vector is only scientifically comparable with another vector produced by the same feature definition.

`SchemaVersion` alone is not enough. Two pipelines may both be version `v1.2` while using different momentum lookbacks, volatility windows, or smoothing settings.

## Descriptor

`StateSchemaDescriptor` records:

- schema id;
- schema version;
- deterministic dimension order;
- feature configuration key/value pairs;
- SHA-256 fingerprint.

## Canonical Fingerprint

The fingerprint is calculated from an invariant UTF-8 text payload with newline separators:

```text
SchemaId=...
SchemaVersion=...
Dimension[0]=...
Dimension[1]=...
Configuration.Key=Value
```

Configuration keys are sorted ordinally. Dimension order is preserved explicitly because vector and matrix construction must not depend on dictionary iteration order.

## Compatibility

Two states are compatible only when their schema fingerprints match. Historical analogue search rejects null schemas and mismatched fingerprints, then reports how many candidates were rejected.

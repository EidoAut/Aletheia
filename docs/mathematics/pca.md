# PCA

## Purpose

Principal component analysis supports state-space exploration by projecting correlated state dimensions into lower-dimensional coordinates.

## Milestone 1 Implementation

The first implementation fits only the first principal component using:

1. feature centering;
2. optional standardization;
3. covariance matrix construction;
4. power iteration for the dominant eigenvector;
5. projection of observations onto the first component.

## Limitations

This is infrastructure, not a complete PCA subsystem. Future milestones should support multiple components, explained-variance tables, whitening options, and stable transform objects for walk-forward validation.

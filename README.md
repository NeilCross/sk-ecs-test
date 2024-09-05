## Integration between Stereokit and Arch ECS

## Objective

Review the performance of an ECS system against a naive LINQ implementation.

Given a simple gravitational physics sample with 100 bodies, model this and determine performance characteristics of each approach.

## Outcome 

We saw relatively decent performance improvements by using the Arch ECS component for managing these simple game objects. Roughly a 4.5x speedup was measured over a List-based LINQ approach.

```
ArchStepper Step took 1.0552ms
LinqStepper Step took 4.9402ms
```

## Preview
![Preview](Docs\sk_grab.gif)

### References:
* Arch ECS - https://github.com/genaray/Arch
* StereoKit - https://github.com/StereoKit/StereoKit
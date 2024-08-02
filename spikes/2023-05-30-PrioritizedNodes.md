# Spike: Prioritized Nodes

## Trace back references

### Conducted by: Anders E Aabakken

### Backlog Work Item: {Link to the work item to provide more context}

### Sprint: {Which sprint did the study take place. Include sprint start date}

## NOTE

This spike has already been followed up by related spikes & stories that has brought this work further. Keeping document for backtracking references.

## Goal {Describe what question(s) the spike intends to answer and why.}

We want to investigate whether it is possible to separate out specific nodes and store them in "prioritized sectors", making them available for loading at a higher priority than other nodes, preferably at all times.

As a test case, we'll use nodes of Discipline 'PIPE', as well as Type VALVE (VALV) and FLANGE (FLAN) from within the 'PIPE' discipline. We do have a business case that is likely to gain from having such nodes prioritized.

By prioritizing nodes, we can ensure that they are loaded at all times (if required), making them available for selection and highlighting throughout a model, even if located at a distance far, far away.

This spike looks the possibility to create such sectors. It is out of scope of this spike to identify the client side rules and/or ways of deciding when and at what priority such sectors should be loaded.

Note that the client has an interface for handing prioritized nodes, but it is currently not implemented. Might investigate whether it would serve useful for this case.

## Method

The idea is that, given input providing priorities - by looking at attributes, we should be able to identify nodes to prioritize into prioritized sectors.

This spike loads the actual nodes into prioritized sectors, it might be worth looking into whether a simplified (LOD) version could be loaded at all times, when high details are not required. This is described in a separate spike.

## Evidence

Document the evidence collected that informed the conclusions below. Examples may include:

Recorded or live demos of a prototype providing the desired capabilities
Metrics collected while testing the prototype
Documentation that indicates the solution can provided the desired capabilities

By adding the attributes of the nodes to the processing, we are able to extract nodes of any given discipline and type. These are easily extractable into separate sectors, that in turn may be identified as high priority sectors.

```C#
var prioritizedTypes = allPrimitives
            .Where(
                x => x.Attributes != null && x.Attributes.Discipline == "PIPE"
                && (x.Attributes.Type == "VALV" || x.Attributes.Type == "FLAN")
            )
            .ToArray();
```

It might be an idea to keep the splitting of prioritized sectors at a different level than the rest, as they are possibly expected to be loaded at all times.

## Conclusions

What was the answer to the question(s) outlined at the start of the spike? Capture what was learned that will inform future work.

The conclusion is that it is indeed possible to create some sort of prioritized sectors based on the knowledge we have at hand during the creation of an asset.

## Next Steps

What work is expected as an outcome of the learning within this spike. Was there work that was blocked or dependent on the learning within this spike?

The spike does NOT take into consideration the increased amount of memory required in order to generate the prioritized sectors. There are multiple ways of doing the actual implementation, some might increase memory consumption, others are likely to demand more processing power. (and thus longer crunch time)

# Flight behavior

`FlyingEntityAgent` and the `flight` behavior give an entity controlled, creative-style flight without replacing Vintage Story's general entity physics or TaskAI system.

For an autonomous flying creature, attach `flight` on both client and server. The client copy observes the synced `feverstonewilds:isFlying` state, while the server copy applies movement and optionally alternates between grounded and flying periods.

```json
"class": "FlyingEntityAgent",
"server": {
  "behaviors": [
    { "code": "controlledphysics" },
    {
      "code": "flight",
      "autoFlight": true,
      "minGroundSeconds": 12,
      "maxGroundSeconds": 28,
      "minFlightSeconds": 10,
      "maxFlightSeconds": 20,
      "landingSpeed": 0.035,
      "landingHeight": 0.15,
      "landingArrivalDistance": 0.05,
      "landingScanDepth": 80
    },
    {
      "code": "taskai",
      "aitasks": [
        { "code": "flightwander", "slot": 0, "priority": 1.9 },
        { "code": "wander", "slot": 0, "priority": 1.0 }
      ]
    }
  ]
}
```

The flight tasks only run while `feverstonewilds:isFlying` is true. Because the flight and ground movement tasks share slot `0`, the higher-priority flight task controls movement in the air, and the existing ground task resumes as soon as the creature lands. Combat tasks can remain in their current slots.

The main tuning values are `flightSpeed`, `verticalSpeed`, `steering`, and `arrivalDistance`. `steering` controls how sharply motion approaches the desired direction; it is the intended place to add more natural acceleration or drag later.

When an automatic flight interval ends, the creature remains in flight while it scans downward for the first solid, non-liquid block. It descends toward a point `landingHeight` blocks above that surface at `landingSpeed`, then restores gravity once it is within `landingArrivalDistance` of that safe landing point. If no suitable surface is found within `landingScanDepth`, it remains airborne instead of falling.

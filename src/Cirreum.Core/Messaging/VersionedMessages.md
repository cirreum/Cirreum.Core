# Message Versioning Best Practices

When creating new versions of existing messages:

1. Keep the same message Identifier
2. Increment the Version
3. Name the class with a version suffix (e.g., `UserCreatedV2`)
4. Document the changes between versions
5. Once enough time has passed and you're certain an older version is no longer in use, it can be safely removed in a future deployment

## Example

```csharp
// Message identifiers (single source of truth for User model related messages)
public static class UserMessages {
    public const string Created = "user.created";
}

// Version 1
[MessageDefinition(UserMessages.Created, "1", MessageTarget.Topic)]
public record UserCreated(string Username, string Email) 
    : DistributedMessage;

// Version 2 (added new fields)
[MessageDefinition(UserMessages.Created, "2", MessageTarget.Topic)]
public record UserCreatedV2(
    string Username, 
    string Email, 
    string DisplayName, 
    bool IsVerified) 
    : DistributedMessage;
```

## Key Points

- **Identifier stays constant** - `"user.created"` for all versions
- **Version increments** - `"1"` → `"2"`
- **Class name reflects version** - `UserCreated` → `UserCreatedV2`
- **Target can differ per version** - If routing requirements change, you can change the target (though this should be rare)
- **Backward compatibility** - Consumers should handle multiple versions gracefully

## Migration Strategy

1. **Deploy V2** alongside V1
2. **Update publishers** to send V2
3. **Update consumers** to handle both V1 and V2
4. **Monitor** for V1 usage to drop to zero
5. **Remove V1** once confirmed unused

## Changing Message Targets

Changing a message's target (Queue ↔ Topic) is a **breaking infrastructure change** and should be treated as a new version.

### ❌ Don't Do This
```csharp
// Changing target on existing version breaks consumers
[MessageDefinition("payment.processed", "1", MessageTarget.Queue)] // Was Topic!
```

### ✅ Do This Instead
```csharp
// Version 1 - Original target
[MessageDefinition("payment.processed", "1", MessageTarget.Topic)]
public record PaymentProcessed(Guid PaymentId) : DistributedMessage;

// Version 2 - New target
[MessageDefinition("payment.processed", "2", MessageTarget.Queue)]
public record PaymentProcessedV2(Guid PaymentId) : DistributedMessage;
```

### Migration Steps

1. **Deploy V2** with new target alongside V1
2. **Dual-publish** to both V1 and V2 temporarily (if feasible)
3. **Migrate consumers** to V2
4. **Monitor** V1 usage to confirm zero traffic
5. **Remove V1** infrastructure and code

### Alternative: New Identifier

If the target change reflects a semantic change (command → event, or vice versa), consider using a new identifier:
```csharp
// Command (queue) - tells system to do something
[MessageDefinition("order.process", "1", MessageTarget.Queue)]
public record ProcessOrder(Guid OrderId) : DistributedMessage;

// Event (topic) - announces something happened
[MessageDefinition("order.processed", "1", MessageTarget.Topic)]
public record OrderProcessed(Guid OrderId, DateTime ProcessedAt) : DistributedMessage;
```

## Anti-Patterns to Avoid

- ❌ **Don't modify existing versions** - Create a new version instead
- ❌ **Don't reuse version numbers** - Each version should be unique
- ❌ **Don't change the identifier** - It should remain stable across versions
- ❌ **Don't skip version numbers** - Increment sequentially for clarity
# C# Game Engine Development Project

## Knowledge Graph Integration
When you see "remember this" in my messages, use the memory-graph tools to store that information. When you see questions about "what we know about X," check the knowledge graph before answering. Actively update the memory when I share new project information.

## Software Architecture
Our engine uses an Entity Component System (ECS) architecture:
- Entities are lightweight IDs
- Components are pure data containers  
- Systems process entities with specific components

## Memory Instructions

### Creating Entities
When I describe a new component, system, or feature, create corresponding entities in the knowledge graph with these properties:
- Name: [System/Component Name]
- Type: [Component/System/Feature]
- Observations: [Key characteristics]

### Creating Relationships
When discussing how components interact, create these relationships in the knowledge graph:
- [Component] depends_on [OtherComponent]
- [Component] interacts_with [OtherComponent]
- [Component] implements [Pattern/Interface]

### Reading Knowledge
Before answering questions about our architecture or implementation details, check the knowledge graph for relevant information.

### File Index
For any code file we discuss, store:
- File path
- Core purpose
- Key components/classes
- Dependencies

## Common Development Patterns

### Two-Phase Implementation
When I request a new feature or implementation, automatically:
1. First analyze the request and present a plan
2. After my feedback, proceed with implementation

### Documentation Requirements
When writing public APIs:
- Include XML documentation for public members
- Document parameters, return values, and exceptions
- Match documentation style with existing code

### Consistency Checking
When updating code or documentation:
- Check for consistency between implementation and documentation
- Verify that architectural patterns are consistently applied
- Alert me to inconsistencies between what's implemented and what's in our memory

## Common Commands
- Check what we know about [component]: Retrieve information
- Remember this about [component]: Update knowledge
- How does [componentA] relate to [componentB]?: Check relationships
- What files implement [component]?: Check file index

## Implementation Notes
When asked to implement code, automatically check our knowledge graph for:
- Existing patterns to follow
- Related components and dependencies
- Code style and naming conventions

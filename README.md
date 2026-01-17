# CopyComponentAsText

CopyComponentAsText is a lightweight Unity utility for exporting component data from GameObjects as readable text. It’s intended for sharing or migrating component setups between different Unity projects without needing to move entire assets or scenes.

## What it does
- Serializes common component fields into a text format.
- Lets you copy component data from one project and paste it into another.
- Keeps the output human-readable so you can diff, review, or tweak values as needed.

## Typical use cases
- Move inspector configurations between projects.
- Share example component setups with teammates.
- Create quick backups of component settings while refactoring.

## Project files
- `ComponentAsText.cs` contains the main export logic.
- `SerializableComponent.cs` and `XMLSerializableObject.cs` handle serialization helpers.

## License
See [LICENSE](LICENSE).

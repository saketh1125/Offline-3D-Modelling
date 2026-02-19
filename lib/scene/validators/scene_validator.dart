import 'package:on_device_3d_builder/core/errors/validation_exception.dart';

/// Strict JSON schema validator for Scene Schema v1.0.
///
/// Validates raw JSON maps against the scene schema specification
/// before they are passed to the rendering engine. This acts as a
/// protection layer ensuring only well-formed data reaches the engine.
///
/// All validation is read-only — the input map is never mutated.
class SceneValidator {
  /// The current supported schema version.
  static const String _supportedSchemaVersion = '1.0';

  /// The set of required root-level keys.
  static const Set<String> _requiredRootKeys = {
    'schema_version',
    'engine_target',
    'metadata',
    'scene_environment',
    'camera',
    'lighting',
    'materials',
    'objects',
  };

  /// Valid primitive types for object geometry.
  static const Set<String> _validPrimitives = {
    'cube',
    'sphere',
    'cylinder',
    'plane',
    'dome',
    'arch',
  };

  /// Validates a raw JSON map against Scene Schema v1.0.
  ///
  /// Throws [ValidationException] on the first encountered error.
  /// The input [json] is never mutated.
  static void validateStrict(Map<String, dynamic> json) {
    _validateRootKeys(json);
    _validateSchemaVersion(json);
    _validateMaterials(json);
    _validateObjects(json);
  }

  // ---------------------------------------------------------------------------
  // Root-Level Validation
  // ---------------------------------------------------------------------------

  /// Ensures all required keys are present and no unknown keys exist.
  static void _validateRootKeys(Map<String, dynamic> json) {
    // Check for missing required keys.
    for (final key in _requiredRootKeys) {
      if (!json.containsKey(key)) {
        throw ValidationException(
          message: 'Missing required root-level key: "$key".',
          fieldName: key,
          reason: 'Required by Scene Schema v$_supportedSchemaVersion.',
        );
      }
    }

    // Reject unknown keys.
    for (final key in json.keys) {
      if (!_requiredRootKeys.contains(key)) {
        throw ValidationException(
          message: 'Unknown root-level key: "$key".',
          fieldName: key,
          reason: 'Only keys defined in the schema are allowed.',
        );
      }
    }
  }

  /// Ensures `schema_version` equals the supported version.
  static void _validateSchemaVersion(Map<String, dynamic> json) {
    final version = json['schema_version'];
    if (version != _supportedSchemaVersion) {
      throw ValidationException(
        message:
            'Unsupported schema version: "$version". Expected "$_supportedSchemaVersion".',
        fieldName: 'schema_version',
        reason: 'Only version $_supportedSchemaVersion is supported.',
      );
    }
  }

  // ---------------------------------------------------------------------------
  // Materials Validation
  // ---------------------------------------------------------------------------

  /// Validates the `materials` list and each material entry.
  static void _validateMaterials(Map<String, dynamic> json) {
    final materials = json['materials'];

    if (materials is! List || materials.isEmpty) {
      throw ValidationException(
        message: '"materials" must be a non-empty list.',
        fieldName: 'materials',
      );
    }

    final seenIds = <String>{};

    for (int i = 0; i < materials.length; i++) {
      final material = materials[i];
      if (material is! Map<String, dynamic>) {
        throw ValidationException(
          message: 'Material at index $i must be a JSON object.',
          fieldName: 'materials[$i]',
        );
      }

      // Require `id`.
      if (!material.containsKey('id') || material['id'] is! String) {
        throw ValidationException(
          message: 'Material at index $i is missing a valid "id" (String).',
          fieldName: 'materials[$i].id',
        );
      }

      final id = material['id'] as String;

      // Check for duplicate material IDs.
      if (seenIds.contains(id)) {
        throw ValidationException(
          message: 'Duplicate material id: "$id".',
          fieldName: 'materials[$i].id',
          reason: 'Each material must have a unique id.',
        );
      }
      seenIds.add(id);

      // Validate `base_color` if present.
      _validateBaseColor(material, i);
    }
  }

  /// Validates that `base_color`, if present, is a 3-element numeric list.
  static void _validateBaseColor(Map<String, dynamic> material, int index) {
    if (!material.containsKey('base_color')) return;

    final baseColor = material['base_color'];
    if (baseColor is! List || baseColor.length != 3) {
      throw ValidationException(
        message:
            'Material at index $index: "base_color" must be a 3-element array [r, g, b].',
        fieldName: 'materials[$index].base_color',
      );
    }

    for (int c = 0; c < 3; c++) {
      if (baseColor[c] is! num) {
        throw ValidationException(
          message:
              'Material at index $index: "base_color[$c]" must be a number.',
          fieldName: 'materials[$index].base_color[$c]',
        );
      }
    }
  }

  // ---------------------------------------------------------------------------
  // Objects Validation
  // ---------------------------------------------------------------------------

  /// Validates the `objects` list and each object entry.
  static void _validateObjects(Map<String, dynamic> json) {
    final objects = json['objects'];

    if (objects is! List || objects.isEmpty) {
      throw ValidationException(
        message: '"objects" must be a non-empty list.',
        fieldName: 'objects',
      );
    }

    final seenIds = <String>{};

    for (int i = 0; i < objects.length; i++) {
      final obj = objects[i];
      if (obj is! Map<String, dynamic>) {
        throw ValidationException(
          message: 'Object at index $i must be a JSON object.',
          fieldName: 'objects[$i]',
        );
      }

      _validateObjectId(obj, i, seenIds);
      _validateObjectGeometry(obj, i);
      _validateObjectMaterialRef(obj, i);
    }
  }

  /// Validates object `id` uniqueness and presence.
  static void _validateObjectId(
      Map<String, dynamic> obj, int index, Set<String> seenIds) {
    if (!obj.containsKey('id') || obj['id'] is! String) {
      throw ValidationException(
        message: 'Object at index $index is missing a valid "id" (String).',
        fieldName: 'objects[$index].id',
      );
    }

    final id = obj['id'] as String;
    if (seenIds.contains(id)) {
      throw ValidationException(
        message: 'Duplicate object id: "$id".',
        fieldName: 'objects[$index].id',
        reason: 'Each object must have a unique id.',
      );
    }
    seenIds.add(id);
  }

  /// Validates that `geometry` is present and contains a valid `primitive`.
  static void _validateObjectGeometry(Map<String, dynamic> obj, int index) {
    if (!obj.containsKey('geometry') || obj['geometry'] is! Map) {
      throw ValidationException(
        message: 'Object at index $index is missing "geometry" (Map).',
        fieldName: 'objects[$index].geometry',
      );
    }

    final geometry = obj['geometry'] as Map;
    if (!geometry.containsKey('primitive') ||
        geometry['primitive'] is! String) {
      throw ValidationException(
        message:
            'Object at index $index: "geometry.primitive" must be a String.',
        fieldName: 'objects[$index].geometry.primitive',
      );
    }

    final primitive = geometry['primitive'] as String;
    if (!_validPrimitives.contains(primitive)) {
      throw ValidationException(
        message: 'Object at index $index: unknown primitive "$primitive". '
            'Valid: ${_validPrimitives.join(", ")}.',
        fieldName: 'objects[$index].geometry.primitive',
      );
    }
  }

  /// Validates that `material_ref` is present and is a non-empty String.
  static void _validateObjectMaterialRef(Map<String, dynamic> obj, int index) {
    if (!obj.containsKey('material_ref') || obj['material_ref'] is! String) {
      throw ValidationException(
        message: 'Object at index $index is missing "material_ref" (String).',
        fieldName: 'objects[$index].material_ref',
      );
    }

    final ref = obj['material_ref'] as String;
    if (ref.isEmpty) {
      throw ValidationException(
        message: 'Object at index $index: "material_ref" cannot be empty.',
        fieldName: 'objects[$index].material_ref',
      );
    }
  }
}

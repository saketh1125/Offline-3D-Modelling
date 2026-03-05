class SceneModel {
  final String id;
  final String name;
  final Map<String, dynamic> data;

  SceneModel({
    required this.id,
    required this.name,
    required this.data,
  });

  factory SceneModel.fromJson(Map<String, dynamic> json) {
    return SceneModel(
      id: json['id'] as String,
      name: json['name'] as String,
      data: json['data'] as Map<String, dynamic>,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'data': data,
    };
  }
}

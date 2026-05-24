/// Safely reads PostgREST embedded objects (COURSES, USER, etc.).
class JsonEmbed {
  static String field(Map<String, dynamic> row, String embedKey, String fieldName) {
    final embed = row[embedKey];
    if (embed is Map) {
      return embed[fieldName]?.toString() ?? '';
    }
    if (embed is List && embed.isNotEmpty && embed.first is Map) {
      return (embed.first as Map)[fieldName]?.toString() ?? '';
    }
    return '';
  }

  static String nested(
    Map<String, dynamic> row,
    List<String> path,
  ) {
    if (path.isEmpty) return '';
  dynamic cur = row;
    for (var i = 0; i < path.length - 1; i++) {
      cur = _child(cur, path[i]);
      if (cur == null) return '';
    }
    if (cur is! Map) return '';
    return cur[path.last]?.toString() ?? '';
  }

  static dynamic _child(dynamic token, String key) {
    if (token is Map) return token[key];
    if (token is List && token.isNotEmpty && token.first is Map) {
      return (token.first as Map)[key];
    }
    return null;
  }

  static const statusNames = {
    1: 'present',
    2: 'suspicious',
    3: 'manual_override',
    4: 'absent',
  };
}

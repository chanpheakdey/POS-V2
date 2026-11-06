import 'dart:ui';

/// Predefined color array for category and group card backgrounds
/// Matches the exact 21-color array from the backend
const List<String> kCategoryColors = [
  '#04478a',
  '#8a040a',
  '#8a045b',
  '#6f048a',
  '#31048a',
  '#04148a',
  '#045f8a',
  '#048a71',
  '#048a27',
  '#3e8a04',
  '#888a04',
  '#8a4804',
  '#8a1704',
  '#b32020',
  '#b32071',
  '#9620b3',
  '#3d20b3',
  '#2068b3',
  '#20b3a5',
  '#20b339',
  '#4bb320',
];

/// Get a color for a given index using the predefined color array
/// Colors cycle through the array using modulo operation
Color colorForIndex(int index) {
  final hexColor = kCategoryColors[index % kCategoryColors.length];
  return Color(int.parse(hexColor.replaceFirst('#', '0xFF')));
}

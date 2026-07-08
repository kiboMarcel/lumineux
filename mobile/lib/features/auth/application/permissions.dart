/// Libellés FR des droits de gestion (codes renvoyés par `GET /auth/me`).
const Map<String, String> _permissionLabels = {
  'manage_attendance': 'Gérer les présences',
  'manage_members': 'Gérer les membres',
  'manage_bureau_profiles': 'Gérer les profils du bureau',
  'manage_referentials': 'Gérer les référentiels',
};

/// Vrai si l'utilisateur possède au moins un droit de gestion (rôle « Bureau »).
bool hasManagementRights(List<String> permissions) =>
    permissions.any((p) => p.startsWith('manage_'));

/// Rôle effectif dérivé des droits (jamais un rôle figé — cf. handoff design).
String roleLabel(List<String> permissions) =>
    hasManagementRights(permissions) ? 'Bureau' : 'Membre';

/// Traduit les codes de droits en libellés FR affichables.
List<String> permissionLabels(List<String> permissions) => permissions
    .map((p) => _permissionLabels[p] ?? p)
    .toList(growable: false);

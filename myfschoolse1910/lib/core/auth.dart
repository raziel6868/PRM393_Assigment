import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/api_client.dart';

enum UserRole { administrator, teacher, parent, student }

class Session {
  final String displayName;
  final List<UserRole> roles;
  final bool passwordChangeRequired;

  Session({
    required this.displayName,
    required this.roles,
    this.passwordChangeRequired = false,
  });

  UserRole get primaryRole => roles.isNotEmpty ? roles.first : UserRole.student;

  bool get isTeacher => roles.contains(UserRole.teacher);
  bool get isParent => roles.contains(UserRole.parent);
  bool get isStudent => roles.contains(UserRole.student);
  bool get isAdministrator => roles.contains(UserRole.administrator);
}

class AuthState {
  final Session? session;
  final bool isLoading;
  final String? error;

  AuthState({this.session, this.isLoading = false, this.error});

  bool get isAuthenticated => session != null;
}

class AuthNotifier extends StateNotifier<AuthState> {
  final ApiClient _apiClient;

  AuthNotifier(this._apiClient) : super(AuthState());

  Future<bool> signIn(String emailOrUserName, String password) async {
    state = AuthState(isLoading: true);
    try {
      final response = await _apiClient.signIn(emailOrUserName, password);
      if (response.statusCode == 200) {
        final data = response.data;

        await _apiClient.saveTokens(
          accessToken: data['accessToken'],
          refreshToken: data['refreshToken'],
        );

        final roles = (data['roles'] as List)
            .map(
              (value) => UserRole.values.cast<UserRole?>().firstWhere(
                (role) =>
                    role?.name.toLowerCase() == value.toString().toLowerCase(),
                orElse: () => null,
              ),
            )
            .whereType<UserRole>()
            .toList();
        if (roles.isEmpty) {
          state = AuthState(error: 'Tài khoản không được phép dùng ứng dụng.');
          await _apiClient.clearTokens();
          return false;
        }

        state = AuthState(
          session: Session(
            displayName: data['displayName'] ?? '',
            roles: roles,
            passwordChangeRequired: data['passwordChangeRequired'] ?? false,
          ),
        );
        return true;
      }
    } catch (e) {
      state = AuthState(error: e.toString());
    }
    return false;
  }

  Future<bool> requestPasswordHelp(String emailOrUserName) async {
    try {
      await _apiClient.requestPasswordHelp(emailOrUserName);
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<bool> changeTemporaryPassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    state = AuthState(isLoading: true, session: state.session);
    try {
      await _apiClient.changeTemporaryPassword(
        currentPassword: currentPassword,
        newPassword: newPassword,
      );
      await logout();
      return true;
    } catch (e) {
      state = AuthState(
        isLoading: false,
        session: state.session,
        error: e.toString(),
      );
      return false;
    }
  }

  Future<void> logout() async {
    try {
      await _apiClient.logout();
    } finally {
      state = AuthState();
    }
  }

  Future<bool> tryRestoreSession() async {
    final token = await _apiClient.getAccessToken();
    if (token == null) return false;
    try {
      final response = await _apiClient.getSession();
      final data = response.data as Map<String, dynamic>;
      final roles = (data['roles'] as List)
          .map(
            (value) => UserRole.values.cast<UserRole?>().firstWhere(
              (role) =>
                  role?.name.toLowerCase() == value.toString().toLowerCase(),
              orElse: () => null,
            ),
          )
          .whereType<UserRole>()
          .toList();
      if (roles.isEmpty) {
        await _apiClient.clearTokens();
        return false;
      }
      state = AuthState(
        session: Session(
          displayName: data['displayName']?.toString() ?? '',
          roles: roles,
          passwordChangeRequired: data['passwordChangeRequired'] == true,
        ),
      );
      return true;
    } catch (_) {
      await _apiClient.clearTokens();
      state = AuthState();
      return false;
    }
  }

  void clearError() {
    state = AuthState(session: state.session, isLoading: state.isLoading);
  }
}

final apiClientProvider = Provider<ApiClient>((ref) => ApiClient());
final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier(ref.watch(apiClientProvider))..tryRestoreSession();
});

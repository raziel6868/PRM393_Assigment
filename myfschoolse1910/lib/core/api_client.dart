import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ApiClient {
  static const String _baseUrl = String.fromEnvironment(
    'FLUTTER_API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5080/api/v1',
  );

  static const String _accessTokenKey = 'access_token';
  static const String _refreshTokenKey = 'refresh_token';

  late final Dio _dio;
  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  ApiClient() {
    _dio = Dio(
      BaseOptions(
        baseUrl: _baseUrl,
        connectTimeout: const Duration(seconds: 30),
        receiveTimeout: const Duration(seconds: 30),
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      ),
    );

    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await _storage.read(key: _accessTokenKey);
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          return handler.next(options);
        },
        onError: (error, handler) async {
          if (error.response?.statusCode == 401) {
            final refreshed = await _refreshToken();
            if (refreshed) {
              final token = await _storage.read(key: _accessTokenKey);
              error.requestOptions.headers['Authorization'] = 'Bearer $token';
              final response = await _dio.fetch(error.requestOptions);
              return handler.resolve(response);
            }
          }
          return handler.next(error);
        },
      ),
    );
  }

  Future<bool> _refreshToken() async {
    try {
      final refreshToken = await _storage.read(key: _refreshTokenKey);
      if (refreshToken == null) return false;

      final response = await Dio().post(
        '$_baseUrl/auth/refresh',
        data: {'clientType': 'mobile', 'refreshToken': refreshToken},
      );

      if (response.statusCode == 200) {
        final data = response.data;
        await _storage.write(key: _accessTokenKey, value: data['accessToken']);
        await _storage.write(
          key: _refreshTokenKey,
          value: data['refreshToken'],
        );
        return true;
      }
    } catch (_) {}
    return false;
  }

  Future<void> saveTokens({
    required String accessToken,
    String? refreshToken,
  }) async {
    await _storage.write(key: _accessTokenKey, value: accessToken);
    if (refreshToken == null) {
      await _storage.delete(key: _refreshTokenKey);
    } else {
      await _storage.write(key: _refreshTokenKey, value: refreshToken);
    }
  }

  Future<void> clearTokens() async {
    await _storage.delete(key: _accessTokenKey);
    await _storage.delete(key: _refreshTokenKey);
  }

  Future<String?> getAccessToken() async {
    return await _storage.read(key: _accessTokenKey);
  }

  // Auth endpoints
  Future<Response> signIn(String emailOrUserName, String password) async {
    return await _dio.post(
      '/auth/sign-in',
      data: {
        'emailOrUserName': emailOrUserName,
        'password': password,
        'clientType': 'mobile',
      },
    );
  }

  Future<Response> requestPasswordHelp(String emailOrUserName) async {
    return await _dio.post(
      '/auth/password-help-requests',
      data: {'emailOrUserName': emailOrUserName},
    );
  }

  Future<Response> changeTemporaryPassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    return await _dio.post(
      '/auth/change-temporary-password',
      data: {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
        'confirmation': newPassword,
      },
    );
  }

  // Me endpoints
  Future<Response> getClasses() async {
    return await _dio.get('/me/classes');
  }

  // Grades endpoints
  Future<Response> getSemesters() async {
    return await _dio.get('/grades/semesters');
  }

  Future<Response> getGradeSummary(String semesterId) async {
    return await _dio.get('/grades/summary/$semesterId');
  }

  Future<Response> getGradeDetail(String gradeId) async {
    return await _dio.get('/grades/$gradeId');
  }

  // Schedule/Timetable endpoints
  Future<Response> getWeeklyTimetable(DateTime weekStart) async {
    final date =
        '${weekStart.year.toString().padLeft(4, '0')}-'
        '${weekStart.month.toString().padLeft(2, '0')}-'
        '${weekStart.day.toString().padLeft(2, '0')}';
    return await _dio.get(
      '/schedule/weekly',
      queryParameters: {'weekStart': date},
    );
  }

  // Events endpoints
  Future<Response> getEvents({int page = 1, int pageSize = 20}) async {
    return await _dio.get(
      '/events',
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
  }

  Future<Response> getEventDetail(String eventId) async {
    return await _dio.get('/events/$eventId');
  }

  // Leave Requests endpoints
  Future<Response> getLeaveRequests({
    int page = 1,
    int pageSize = 20,
    String? status,
  }) async {
    return await _dio.get(
      '/leave-requests',
      queryParameters: {
        'page': page,
        'pageSize': pageSize,
        if (status != null) 'status': status,
      },
    );
  }

  Future<Response> getLeaveRequestDetail(String requestId) async {
    return await _dio.get('/leave-requests/$requestId');
  }

  Future<Response> createLeaveRequest(Map<String, dynamic> data) async {
    return await _dio.post('/leave-requests', data: data);
  }

  Future<Response> cancelLeaveRequest(
    String requestId,
    String rowVersion,
  ) async {
    return await _dio.post(
      '/leave-requests/$requestId/cancel',
      data: {'rowVersion': rowVersion},
    );
  }

  // Clubs endpoints
  Future<Response> getClubs({
    int page = 1,
    int pageSize = 20,
    String? search,
    String? category,
  }) async {
    return await _dio.get(
      '/clubs',
      queryParameters: {
        'page': page,
        'pageSize': pageSize,
        if (search != null) 'search': search,
        if (category != null) 'category': category,
      },
    );
  }

  Future<Response> getClubDetail(String clubId) async {
    return await _dio.get('/clubs/$clubId');
  }

  Future<Response> joinClub(String clubId) async {
    return await _dio.post('/clubs/$clubId/join');
  }

  Future<Response> leaveClub(String clubId) async {
    return await _dio.post('/clubs/$clubId/leave');
  }

  // Announcements endpoints
  Future<Response> getAnnouncements({int page = 1, int pageSize = 20}) async {
    return await _dio.get(
      '/announcements',
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
  }

  Future<Response> getAnnouncementDetail(String announcementId) async {
    return await _dio.get('/announcements/$announcementId');
  }

  Future<Response> markAnnouncementAsRead(String announcementId) async {
    return await _dio.post('/announcements/$announcementId/read');
  }

  Future<Response> getUnreadAnnouncementCount() async {
    return await _dio.get('/announcements/unread-count');
  }
}

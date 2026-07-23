import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/auth.dart';
import '../../shared/api_view.dart';

class CreateLeaveRequestScreen extends ConsumerStatefulWidget {
  const CreateLeaveRequestScreen({super.key});

  @override
  ConsumerState<CreateLeaveRequestScreen> createState() =>
      _CreateLeaveRequestScreenState();
}

class _CreateLeaveRequestScreenState
    extends ConsumerState<CreateLeaveRequestScreen> {
  final _formKey = GlobalKey<FormState>();
  final _reasonController = TextEditingController();
  late Future<List<Map<String, dynamic>>> _children;
  String? _studentProfileId;
  DateTime _startDate = DateTime.now();
  DateTime _endDate = DateTime.now();
  String _startSession = 'morning';
  String _endSession = 'afternoon';
  String _category = 'health';
  bool _submitting = false;

  @override
  void initState() {
    super.initState();
    _children = _loadChildren();
  }

  @override
  void dispose() {
    _reasonController.dispose();
    super.dispose();
  }

  Future<List<Map<String, dynamic>>> _loadChildren() async {
    final response = await ref.read(apiClientProvider).getClasses();
    final seen = <String>{};
    final children = asMapList(response.data)
        .where((item) => seen.add(item['studentProfileId']?.toString() ?? ''))
        .toList();
    _studentProfileId ??= children.isEmpty
        ? null
        : children.first['studentProfileId']?.toString();
    return children;
  }

  Future<void> _pickDate({required bool start}) async {
    final current = start ? _startDate : _endDate;
    final selected = await showDatePicker(
      context: context,
      initialDate: current,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (selected == null) return;
    setState(() {
      if (start) {
        _startDate = selected;
        if (_endDate.isBefore(selected)) _endDate = selected;
      } else {
        _endDate = selected;
      }
    });
  }

  String _wireDate(DateTime value) =>
      '${value.year.toString().padLeft(4, '0')}-'
      '${value.month.toString().padLeft(2, '0')}-'
      '${value.day.toString().padLeft(2, '0')}';

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate() || _studentProfileId == null) return;
    setState(() => _submitting = true);
    try {
      await ref.read(apiClientProvider).createLeaveRequest({
        'studentProfileId': _studentProfileId,
        'startDate': _wireDate(_startDate),
        'endDate': _wireDate(_endDate),
        'startSession': _startSession,
        'endSession': _endSession,
        'reasonCategory': _category,
        'reason': _reasonController.text.trim(),
      });
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Đã gửi đơn xin nghỉ.')));
        context.pop();
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(apiErrorMessage(error))));
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Tạo đơn xin nghỉ')),
      body: FutureBuilder<List<Map<String, dynamic>>>(
        future: _children,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const LoadingView();
          }
          if (snapshot.hasError) {
            return ErrorView(
              error: snapshot.error!,
              onRetry: () => setState(() => _children = _loadChildren()),
            );
          }
          final children = snapshot.data ?? [];
          if (children.isEmpty) {
            return const EmptyView(
              message: 'Tài khoản chưa được liên kết với học sinh.',
              icon: Icons.child_care_outlined,
            );
          }
          return Form(
            key: _formKey,
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                DropdownButtonFormField<String>(
                  initialValue: _studentProfileId,
                  decoration: const InputDecoration(labelText: 'Học sinh'),
                  items: children
                      .map(
                        (child) => DropdownMenuItem(
                          value: child['studentProfileId']?.toString(),
                          child: Text(
                            '${child['studentDisplayName']} · '
                            '${child['classDisplayName']}',
                          ),
                        ),
                      )
                      .toList(),
                  onChanged: (value) => _studentProfileId = value,
                ),
                const SizedBox(height: 16),
                Row(
                  children: [
                    Expanded(
                      child: _DateField(
                        label: 'Từ ngày',
                        value: formatDate(_startDate.toIso8601String()),
                        onTap: () => _pickDate(start: true),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _DateField(
                        label: 'Đến ngày',
                        value: formatDate(_endDate.toIso8601String()),
                        onTap: () => _pickDate(start: false),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                Row(
                  children: [
                    Expanded(
                      child: _SessionField(
                        label: 'Buổi bắt đầu',
                        value: _startSession,
                        onChanged: (value) =>
                            setState(() => _startSession = value!),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _SessionField(
                        label: 'Buổi kết thúc',
                        value: _endSession,
                        onChanged: (value) =>
                            setState(() => _endSession = value!),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                DropdownButtonFormField<String>(
                  initialValue: _category,
                  decoration: const InputDecoration(labelText: 'Loại lý do'),
                  items: const [
                    DropdownMenuItem(value: 'health', child: Text('Sức khoẻ')),
                    DropdownMenuItem(value: 'family', child: Text('Gia đình')),
                    DropdownMenuItem(value: 'personal', child: Text('Cá nhân')),
                    DropdownMenuItem(value: 'academic', child: Text('Học tập')),
                    DropdownMenuItem(value: 'other', child: Text('Khác')),
                  ],
                  onChanged: (value) => setState(() => _category = value!),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _reasonController,
                  minLines: 4,
                  maxLines: 6,
                  maxLength: 500,
                  decoration: const InputDecoration(
                    labelText: 'Lý do chi tiết',
                    alignLabelWithHint: true,
                  ),
                  validator: (value) {
                    final length = value?.trim().length ?? 0;
                    if (length < 20) {
                      return 'Nội dung phải có ít nhất 20 ký tự.';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 8),
                FilledButton.icon(
                  onPressed: _submitting ? null : _submit,
                  icon: const Icon(Icons.send_outlined),
                  label: Text(_submitting ? 'Đang gửi...' : 'Gửi đơn'),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _DateField extends StatelessWidget {
  final String label;
  final String value;
  final VoidCallback onTap;

  const _DateField({
    required this.label,
    required this.value,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          suffixIcon: const Icon(Icons.calendar_month_outlined),
        ),
        child: Text(value),
      ),
    );
  }
}

class _SessionField extends StatelessWidget {
  final String label;
  final String value;
  final ValueChanged<String?> onChanged;

  const _SessionField({
    required this.label,
    required this.value,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<String>(
      initialValue: value,
      decoration: InputDecoration(labelText: label),
      items: const [
        DropdownMenuItem(value: 'morning', child: Text('Sáng')),
        DropdownMenuItem(value: 'afternoon', child: Text('Chiều')),
      ],
      onChanged: onChanged,
    );
  }
}

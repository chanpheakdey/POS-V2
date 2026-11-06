import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/auth_service.dart';
import '../../auth/auth_result.dart';

/// Login screen widget with username and password fields.
/// 
/// Features:
/// - Username and password text fields with validation
/// - Red border on empty fields when submit is attempted
/// - Loading indicator during authentication
/// - Exact error messages for authentication failures:
///   - "User name is incorrect!" for unknown username
///   - "Password is incorrect!" for wrong password
///   - "Your user account has been locked! Please contact IT" for locked accounts
/// 
/// **Validates: Requirements 1.1, 1.4, 1.6, 13.3, 17.2**
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  
  bool _isLoading = false;
  bool _hasAttemptedSubmit = false;
  String? _errorMessage;

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  /// Validate that field is not empty.
  String? _validateRequired(String? value) {
    if (_hasAttemptedSubmit && (value == null || value.trim().isEmpty)) {
      return ''; // Return empty string to trigger red border
    }
    return null;
  }

  /// Handle login button press.
  Future<void> _handleLogin() async {
    setState(() {
      _hasAttemptedSubmit = true;
      _errorMessage = null;
    });

    // Validate form
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _isLoading = true;
    });

    try {
      final authService = ref.read(authServiceProvider.notifier);
      final result = await authService.login(
        _usernameController.text.trim(),
        _passwordController.text,
      );

      if (!mounted) return;

      if (result is AuthSuccess) {
        // Navigation will be handled by the router based on auth state
        // The auth service has updated the state to authenticated
      } else if (result is AuthFailure) {
        setState(() {
          _errorMessage = result.message;
          _isLoading = false;
        });
      } else if (result is AuthAccountLocked) {
        setState(() {
          _errorMessage = 'Your user account has been locked! Please contact IT';
          _isLoading = false;
        });
      } else if (result is AuthInvalidUserLevel) {
        setState(() {
          _errorMessage = 'This app is for customers only';
          _isLoading = false;
        });
      } else if (result is AuthNetworkError) {
        setState(() {
          _errorMessage = result.message;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (!mounted) return;
      
      setState(() {
        _errorMessage = 'An unexpected error occurred';
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24.0),
            child: Form(
              key: _formKey,
              autovalidateMode: AutovalidateMode.disabled,
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // App Logo or Title
                  const Icon(
                    Icons.shopping_bag,
                    size: 80,
                    color: Colors.blue,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'POS App',
                    style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Customer Login',
                    style: Theme.of(context).textTheme.bodyLarge,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 48),

                  // Username Field
                  TextFormField(
                    controller: _usernameController,
                    decoration: InputDecoration(
                      labelText: 'Username',
                      prefixIcon: const Icon(Icons.person),
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                      errorStyle: const TextStyle(height: 0), // Hide error text
                    ),
                    enabled: !_isLoading,
                    textInputAction: TextInputAction.next,
                    validator: _validateRequired,
                    onChanged: (_) {
                      if (_hasAttemptedSubmit) {
                        _formKey.currentState!.validate();
                      }
                    },
                  ),
                  const SizedBox(height: 16),

                  // Password Field
                  TextFormField(
                    controller: _passwordController,
                    decoration: InputDecoration(
                      labelText: 'Password',
                      prefixIcon: const Icon(Icons.lock),
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                      errorStyle: const TextStyle(height: 0), // Hide error text
                    ),
                    obscureText: true,
                    enabled: !_isLoading,
                    textInputAction: TextInputAction.done,
                    validator: _validateRequired,
                    onChanged: (_) {
                      if (_hasAttemptedSubmit) {
                        _formKey.currentState!.validate();
                      }
                    },
                    onFieldSubmitted: (_) {
                      if (!_isLoading) {
                        _handleLogin();
                      }
                    },
                  ),
                  const SizedBox(height: 24),

                  // Error Message
                  if (_errorMessage != null)
                    Container(
                      padding: const EdgeInsets.all(12),
                      margin: const EdgeInsets.only(bottom: 16),
                      decoration: BoxDecoration(
                        color: Colors.red[50],
                        border: Border.all(color: Colors.red),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        children: [
                          const Icon(Icons.error_outline, color: Colors.red),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Text(
                              _errorMessage!,
                              style: const TextStyle(color: Colors.red),
                            ),
                          ),
                        ],
                      ),
                    ),

                  // Login Button
                  ElevatedButton(
                    onPressed: _isLoading ? null : _handleLogin,
                    style: ElevatedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 16),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                    ),
                    child: _isLoading
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                            ),
                          )
                        : const Text(
                            'Login',
                            style: TextStyle(fontSize: 16),
                          ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

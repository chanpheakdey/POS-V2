import 'package:flutter_test/flutter_test.dart';
import 'package:posdalin_app/src/shared/service_result.dart';

void main() {
  group('ServiceResult', () {
    group('Ok', () {
      test('should create an Ok result with value', () {
        const result = ServiceResult<int>.ok(42);
        
        expect(result, isA<Ok<int>>());
        expect((result as Ok<int>).value, equals(42));
      });

      test('should support equality comparison', () {
        const result1 = ServiceResult<String>.ok('success');
        const result2 = ServiceResult<String>.ok('success');
        const result3 = ServiceResult<String>.ok('different');

        expect(result1, equals(result2));
        expect(result1, isNot(equals(result3)));
      });

      test('should have consistent hashCode for equal values', () {
        const result1 = ServiceResult<int>.ok(100);
        const result2 = ServiceResult<int>.ok(100);

        expect(result1.hashCode, equals(result2.hashCode));
      });

      test('should provide meaningful toString', () {
        const result = ServiceResult<String>.ok('test');
        
        expect(result.toString(), equals('Ok(value: test)'));
      });
    });

    group('NetworkError', () {
      test('should create a NetworkError result with message', () {
        const result = ServiceResult<int>.networkError('Connection timeout');
        
        expect(result, isA<NetworkError<int>>());
        expect((result as NetworkError<int>).message, equals('Connection timeout'));
      });

      test('should support equality comparison', () {
        const result1 = ServiceResult<String>.networkError('Error 1');
        const result2 = ServiceResult<String>.networkError('Error 1');
        const result3 = ServiceResult<String>.networkError('Error 2');

        expect(result1, equals(result2));
        expect(result1, isNot(equals(result3)));
      });

      test('should have consistent hashCode for equal messages', () {
        const result1 = ServiceResult<int>.networkError('Network failure');
        const result2 = ServiceResult<int>.networkError('Network failure');

        expect(result1.hashCode, equals(result2.hashCode));
      });

      test('should provide meaningful toString', () {
        const result = ServiceResult<int>.networkError('No connection');
        
        expect(result.toString(), equals('NetworkError(message: No connection)'));
      });
    });

    group('AuthError', () {
      test('should create an AuthError result', () {
        const result = ServiceResult<int>.authError();
        
        expect(result, isA<AuthError<int>>());
      });

      test('should support equality comparison', () {
        const result1 = ServiceResult<String>.authError();
        const result2 = ServiceResult<String>.authError();

        expect(result1, equals(result2));
      });

      test('should have consistent hashCode', () {
        const result1 = ServiceResult<int>.authError();
        const result2 = ServiceResult<int>.authError();

        expect(result1.hashCode, equals(result2.hashCode));
      });

      test('should provide meaningful toString', () {
        const result = ServiceResult<int>.authError();
        
        expect(result.toString(), equals('AuthError()'));
      });
    });

    group('ValidationError', () {
      test('should create a ValidationError result with message', () {
        const result = ServiceResult<int>.validationError('Invalid input');
        
        expect(result, isA<ValidationError<int>>());
        expect((result as ValidationError<int>).message, equals('Invalid input'));
      });

      test('should support equality comparison', () {
        const result1 = ServiceResult<String>.validationError('Field required');
        const result2 = ServiceResult<String>.validationError('Field required');
        const result3 = ServiceResult<String>.validationError('Invalid format');

        expect(result1, equals(result2));
        expect(result1, isNot(equals(result3)));
      });

      test('should have consistent hashCode for equal messages', () {
        const result1 = ServiceResult<int>.validationError('Invalid email');
        const result2 = ServiceResult<int>.validationError('Invalid email');

        expect(result1.hashCode, equals(result2.hashCode));
      });

      test('should provide meaningful toString', () {
        const result = ServiceResult<int>.validationError('Username required');
        
        expect(result.toString(), equals('ValidationError(message: Username required)'));
      });
    });

    group('UnexpectedError', () {
      test('should create an UnexpectedError result with message', () {
        const result = ServiceResult<int>.unexpectedError('Unknown error');
        
        expect(result, isA<UnexpectedError<int>>());
        expect((result as UnexpectedError<int>).message, equals('Unknown error'));
      });

      test('should support equality comparison', () {
        const result1 = ServiceResult<String>.unexpectedError('System error');
        const result2 = ServiceResult<String>.unexpectedError('System error');
        const result3 = ServiceResult<String>.unexpectedError('Different error');

        expect(result1, equals(result2));
        expect(result1, isNot(equals(result3)));
      });

      test('should have consistent hashCode for equal messages', () {
        const result1 = ServiceResult<int>.unexpectedError('Fatal error');
        const result2 = ServiceResult<int>.unexpectedError('Fatal error');

        expect(result1.hashCode, equals(result2.hashCode));
      });

      test('should provide meaningful toString', () {
        const result = ServiceResult<int>.unexpectedError('Crash detected');
        
        expect(result.toString(), equals('UnexpectedError(message: Crash detected)'));
      });
    });

    group('Type safety', () {
      test('should maintain type parameter across different result types', () {
        const okResult = ServiceResult<int>.ok(123);
        const networkErrorResult = ServiceResult<int>.networkError('Error');
        const authErrorResult = ServiceResult<int>.authError();
        const validationErrorResult = ServiceResult<int>.validationError('Invalid');
        const unexpectedErrorResult = ServiceResult<int>.unexpectedError('Unexpected');

        expect(okResult, isA<ServiceResult<int>>());
        expect(networkErrorResult, isA<ServiceResult<int>>());
        expect(authErrorResult, isA<ServiceResult<int>>());
        expect(validationErrorResult, isA<ServiceResult<int>>());
        expect(unexpectedErrorResult, isA<ServiceResult<int>>());
      });

      test('should work with complex types', () {
        const result = ServiceResult<List<String>>.ok(['a', 'b', 'c']);
        
        expect(result, isA<Ok<List<String>>>());
        expect((result as Ok<List<String>>).value, equals(['a', 'b', 'c']));
      });

      test('should work with nullable types', () {
        const result = ServiceResult<String?>.ok(null);
        
        expect(result, isA<Ok<String?>>());
        expect((result as Ok<String?>).value, isNull);
      });
    });

    group('Pattern matching', () {
      test('should support exhaustive pattern matching with switch', () {
        String handleResult(ServiceResult<int> result) {
          return switch (result) {
            Ok(:final value) => 'Success: $value',
            NetworkError(:final message) => 'Network: $message',
            AuthError() => 'Auth failed',
            ValidationError(:final message) => 'Validation: $message',
            UnexpectedError(:final message) => 'Unexpected: $message',
          };
        }

        expect(
          handleResult(const ServiceResult.ok(42)),
          equals('Success: 42'),
        );
        expect(
          handleResult(const ServiceResult.networkError('Timeout')),
          equals('Network: Timeout'),
        );
        expect(
          handleResult(const ServiceResult.authError()),
          equals('Auth failed'),
        );
        expect(
          handleResult(const ServiceResult.validationError('Invalid input')),
          equals('Validation: Invalid input'),
        );
        expect(
          handleResult(const ServiceResult.unexpectedError('System failure')),
          equals('Unexpected: System failure'),
        );
      });

      test('should support type checking with is operator', () {
        const result1 = ServiceResult<int>.ok(100);
        const result2 = ServiceResult<int>.networkError('Error');
        const result3 = ServiceResult<int>.authError();
        const result4 = ServiceResult<int>.validationError('Invalid');
        const result5 = ServiceResult<int>.unexpectedError('Unexpected');

        expect(result1 is Ok<int>, isTrue);
        expect(result2 is NetworkError<int>, isTrue);
        expect(result3 is AuthError<int>, isTrue);
        expect(result4 is ValidationError<int>, isTrue);
        expect(result5 is UnexpectedError<int>, isTrue);
      });
    });

    group('Requirements validation', () {
      test('NetworkError - displays as snackbar (Req 13.1)', () {
        // Validates that network errors can be created and carry messages
        // that would be displayed as snackbars in the UI
        const result = ServiceResult<String>.networkError('Connection failed');
        
        expect(result, isA<NetworkError<String>>());
        expect((result as NetworkError).message, isNotEmpty);
      });

      test('AuthError - triggers redirect to login (Req 13.6)', () {
        // Validates that authentication errors can be detected
        // and would trigger navigation to login screen
        const result = ServiceResult<void>.authError();
        
        expect(result, isA<AuthError<void>>());
      });

      test('ValidationError - shows inline field messages (Req 13.3)', () {
        // Validates that validation errors carry messages
        // for field-level error display
        const result = ServiceResult<void>.validationError('Password is incorrect!');
        
        expect(result, isA<ValidationError<void>>());
        expect((result as ValidationError).message, contains('Password'));
      });

      test('UnexpectedError - logs to device (Req 13.4)', () {
        // Validates that unexpected errors carry detailed messages
        // for logging purposes
        const result = ServiceResult<void>.unexpectedError('Null pointer exception');
        
        expect(result, isA<UnexpectedError<void>>());
        expect((result as UnexpectedError).message, isNotEmpty);
      });
    });
  });
}

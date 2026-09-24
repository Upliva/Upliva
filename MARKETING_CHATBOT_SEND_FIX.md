# Marketing Chatbot Send-State Fix

## Problem fixed
After a successful lead submission, the server returned success and the thank-you message appeared, but the button remained on `Sending...`, making the chatbot look stuck.

## Fix
- Successful submission now changes the button to `Submitted ✓` and disables it.
- The form is hidden only after the server confirms success.
- A 15-second request timeout prevents an indefinitely pending browser request.
- Non-JSON server responses are handled safely.
- Failed requests restore `Send details` so the visitor can retry.
- No database schema or migration change is required.

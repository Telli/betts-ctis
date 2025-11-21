# Page snapshot

```yaml
- generic [active] [ref=e1]:
  - link "Skip to main content" [ref=e2] [cursor=pointer]:
    - /url: "#main-content"
  - link "Skip to navigation" [ref=e3] [cursor=pointer]:
    - /url: "#navigation"
  - generic [ref=e5]:
    - generic [ref=e6]:
      - img [ref=e8]
      - generic [ref=e10]: Something went wrong
      - generic [ref=e11]: An unexpected error occurred. Please try refreshing the page or contact support if the problem persists.
    - generic [ref=e13]:
      - generic [ref=e14]: Malformed arrow function parameter list
      - button "Try Again" [ref=e15] [cursor=pointer]:
        - img
        - text: Try Again
  - generic [ref=e21] [cursor=pointer]:
    - button "Open issues overlay" [ref=e22] [cursor=pointer]:
      - img [ref=e24] [cursor=pointer]
      - generic [ref=e26] [cursor=pointer]:
        - generic [ref=e27] [cursor=pointer]: "0"
        - generic [ref=e28] [cursor=pointer]: "1"
      - generic [ref=e29] [cursor=pointer]: Issue
    - button "Collapse issues badge" [ref=e30] [cursor=pointer]:
      - img [ref=e31] [cursor=pointer]
  - alert [ref=e33]
```
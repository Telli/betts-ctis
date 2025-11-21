# Page snapshot

```yaml
- generic [active] [ref=e1]:
  - link "Skip to main content" [ref=e2]:
    - /url: "#main-content"
  - link "Skip to navigation" [ref=e3]:
    - /url: "#navigation"
  - generic [ref=e5]:
    - generic [ref=e6]:
      - img [ref=e8]
      - generic [ref=e10]: Something went wrong
      - generic [ref=e11]: An unexpected error occurred. Please try refreshing the page or contact support if the problem persists.
    - generic [ref=e13]:
      - generic [ref=e14]: "Cannot reference undeclared private names: \"#mutationDefaults\""
      - button "Try Again" [ref=e15] [cursor=pointer]:
        - img
        - text: Try Again
  - button "Open Next.js Dev Tools" [ref=e21] [cursor=pointer]:
    - img [ref=e22] [cursor=pointer]
  - alert [ref=e27]: CTIS - Client Tax Information System | The Betts Firm
```
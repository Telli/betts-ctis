# Page snapshot

```yaml
- generic [ref=e1]:
  - link "Skip to main content" [ref=e2]:
    - /url: "#main-content"
  - link "Skip to navigation" [ref=e3]:
    - /url: "#navigation"
  - main "Main content" [ref=e5]:
    - generic [ref=e7]:
      - img "Betts logo" [ref=e9]
      - heading "The Betts Firm CTIS" [level=1] [ref=e10]
      - generic [ref=e11]:
        - generic [ref=e12]:
          - generic [ref=e13]: Log in
          - generic [ref=e14]: Enter your email and password to log in to your account
        - generic [ref=e15]:
          - generic [ref=e16]:
            - generic [ref=e17]:
              - generic [ref=e18]: Email
              - textbox "Email" [active] [ref=e19]
            - generic [ref=e20]:
              - generic [ref=e21]:
                - generic [ref=e22]: Password
                - link "Forgot password?" [ref=e23]:
                  - /url: /forgot-password
              - textbox "Password" [ref=e24]: Associate123!
          - generic [ref=e25]:
            - button "Log in" [ref=e26] [cursor=pointer]
            - generic [ref=e27]:
              - text: Don't have an account?
              - link "Register" [ref=e28]:
                - /url: /register
  - region "Notifications (F8)":
    - list
  - button "Open Next.js Dev Tools" [ref=e34] [cursor=pointer]:
    - img [ref=e35] [cursor=pointer]
  - alert [ref=e40]
```
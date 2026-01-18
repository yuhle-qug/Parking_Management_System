# Page snapshot

```yaml
- generic [ref=e4]:
  - generic [ref=e5]:
    - img "SmartPark" [ref=e7]
    - heading "Đăng nhập" [level=2] [ref=e8]
    - paragraph [ref=e9]: Truy cập hệ thống SmartPark
  - generic [ref=e10]: "Đăng nhập thất bại: Sai tài khoản hoặc mật khẩu"
  - generic [ref=e11]:
    - generic [ref=e12]:
      - generic [ref=e13]: Tài khoản
      - textbox "admin" [ref=e14]
    - generic [ref=e15]:
      - generic [ref=e16]: Mật khẩu
      - textbox "123" [ref=e17]: wrong-password
    - generic [ref=e18]:
      - generic [ref=e19]: Luồng cổng đang vận hành
      - generic [ref=e20]:
        - button "Luồng ô tô" [ref=e21]
        - button "Luồng xe máy" [ref=e22]
      - paragraph [ref=e23]: "Cố định luồng ngay từ đăng nhập: cổng dành cho ô tô hoặc xe máy."
    - generic [ref=e24]:
      - generic [ref=e25]: Chọn cổng đang vận hành
      - generic [ref=e26]:
        - 'button "Cổng Ô tô 01 ID: GATE-IN-CAR-01" [ref=e27]':
          - generic [ref=e28]: Cổng Ô tô 01
          - generic [ref=e29]: "ID: GATE-IN-CAR-01"
        - 'button "Cổng Ô tô 02 ID: GATE-IN-CAR-02" [ref=e30]':
          - generic [ref=e31]: Cổng Ô tô 02
          - generic [ref=e32]: "ID: GATE-IN-CAR-02"
        - 'button "Cổng ra Ô tô 01 ID: GATE-OUT-CAR-01" [ref=e33]':
          - generic [ref=e34]: Cổng ra Ô tô 01
          - generic [ref=e35]: "ID: GATE-OUT-CAR-01"
        - 'button "Cổng ra Ô tô 02 ID: GATE-OUT-CAR-02" [ref=e36]':
          - generic [ref=e37]: Cổng ra Ô tô 02
          - generic [ref=e38]: "ID: GATE-OUT-CAR-02"
    - button "Đăng nhập" [ref=e39]
```
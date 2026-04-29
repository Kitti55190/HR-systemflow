- HrFlow (HR Leave + OT Approval Workflow) ระบบ HR ขนาดเล็กสำหรับ “ยื่นคำขอ + อนุมัติหลายชั้น” ใช้เป็นเดโมพอร์ตฟอริโอ้ได้
- โฟลเดอร์โปรเจค:
  - Backend: hrflow-api
  - Frontend: hrflow-web
ใช้เทคโนโลยี/เครื่องมืออะไร

- Backend : ASP.NET Core Web API (.NET 8) + Entity Framework Core
  - Auth: JWT Bearer Authentication
  - API Docs: Swagger UI
  - DB: ค่าเริ่มต้น SQLite (เปลี่ยนเป็น SQL Server/MySQL ได้จาก config)
- Frontend : React + TypeScript + Vite
  - Routing: react-router-dom
  - Proxy dev: Vite proxy /api ไปหา Backend
ทำอะไรได้บ้าง (ฟีเจอร์หลัก)

- Authentication / Role
  - ล็อกอินด้วย JWT (เก็บ token ที่ฝั่งเว็บ)
  - มีบัญชีเดโม: Employee / Manager / HR (HR ทำหน้าที่เหมือนแอดมินฝั่ง HR)
- Leave Request
  - Employee ยื่นใบลา (Draft → Submit)
  - ดูรายการ “คำขอของฉัน”
  - Approve Workflow 2 ชั้น : Manager (L1) → HR (L2)
  - Approver มี “กล่องอนุมัติ” กด Approve / Return / Reject ได้
- OT Request
  - Employee ขอ OT (Draft → Submit) ระบุช่วงเวลา (datetime)
  - ดูรายการ “OT ของฉัน”
  - Approve Workflow 2 ชั้น : Manager (L1) → HR (L2)
  - OT Inbox สำหรับผู้อนุมัติ กด Approve / Return / Reject ได้
- Notification (ในระบบ)
  - สร้าง notification ตอนมีคำขอใหม่/ผลการอนุมัติ (เก็บใน DB)

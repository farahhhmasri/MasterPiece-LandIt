# LandIt 🎯

**LandIt** is a full-stack interview preparation platform that helps candidates practice smarter and land their next role. It combines AI-powered tools, live mock interview booking, and resume generation in one cohesive platform.

---

## Features

- **AI Resume Analyzer & Generator** — Upload your resume for ATS scoring and feedback, or generate a polished PDF resume powered by Groq (Llama 3.3 70B)
- **AI Interview Question Generator** — Get role-specific interview questions generated dynamically based on job title and context
- **Live Mock Interview Booking** — Candidates can browse recruiter availability and book real-time mock interview sessions
- **Multi-Role System** — Separate dashboards and flows for Candidates, Recruiters, and Admins
- **Stripe Payments** — Secure payment processing for premium features and bookings
- **Dark / Light Theme** — Full theme toggle with a responsive, polished UI

---

## Tech Stack

| Layer | Technologies |
|---|---|
| Backend | ASP.NET Core MVC (.NET 10), Entity Framework Core, SQL Server |
| Auth | ASP.NET Core Identity (multi-role) |
| AI | Groq API, Llama 3.3 70B |
| PDF | QuestPDF |
| Payments | Stripe |
| Frontend | Razor Views, HTML, CSS, JavaScript, Canvas API |


---

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server
- Groq API key
- Stripe API keys

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/farahhhmasri/MasterPiece-LandIt.git
   cd landit
   ```

2. **Configure environment variables** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "your-sql-server-connection-string"
     },
     "Groq": {
       "ApiKey": "your-groq-api-key"
     },
     "Stripe": {
       "PublishableKey": "your-stripe-publishable-key",
       "SecretKey": "your-stripe-secret-key"
     }
   }
   ```

3. **Apply migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

## License

This project is licensed under the MIT License.

# .NET 8 API Configuration for Mobile App

Update your .NET 8 API to support the mobile app.

## 1. Update CORS Configuration

Edit `Program.cs`:

```csharp
// Add CORS policy for mobile app
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
    {
        policy.WithOrigins(
            "capacitor://localhost",
            "ionic://localhost", 
            "http://localhost",
            "http://localhost:8080",
            "http://localhost:4200",
            "https://localhost",
            "http://10.0.2.2:5001" // Android emulator
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// Apply CORS middleware (before UseAuthorization)
app.UseCors("MobileApp");
```

## 2. Test Endpoint

Add a test endpoint to verify connectivity:

```csharp
app.MapGet("/api/test", () => new { 
    message = "API is working", 
    timestamp = DateTime.UtcNow 
})
.RequireCors("MobileApp");
```

## 3. Handle Mobile-Specific Headers

If using authentication, update your JWT configuration to accept mobile origins:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            )
        };
        
        // Allow mobile app origins
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var origin = context.Request.Headers["Origin"].ToString();
                if (origin.StartsWith("capacitor://") || origin.StartsWith("ionic://"))
                {
                    context.Token = context.Request.Headers["Authorization"]
                        .ToString().Replace("Bearer ", "");
                }
                return Task.CompletedTask;
            }
        };
    });
```

## 4. Enable HTTPS (Production)

For production, ensure your API uses HTTPS:

```csharp
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
```

Update mobile app environment:

```typescript
// environment.prod.ts
export const environment = {
  production: true,
  apiUrl: 'https://your-api-domain.com'
};
```

## 5. File Upload Configuration (if needed)

If your app handles file uploads:

```csharp
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760; // 10MB
});
```

## 6. Test API from Mobile

In your Angular service:

```typescript
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

testConnection() {
  return this.http.get(`${environment.apiUrl}/api/test`);
}
```

## 7. Debugging

### Check API is accessible:

```bash
# From Android emulator
adb shell
curl http://10.0.2.2:5001/api/test

# From iOS simulator
curl http://localhost:5001/api/test
```

### Common Issues:

**CORS errors**: Verify origins in CORS policy match Capacitor schemes
**Connection refused**: Check API is running and firewall allows connections
**SSL errors**: Disable SSL validation for development (not production)

## 8. Production Deployment

When deploying to production:

1. Use HTTPS for API
2. Update CORS to only allow your production domain
3. Remove development origins (localhost, 10.0.2.2)
4. Enable rate limiting
5. Add API authentication/authorization
6. Monitor API logs for mobile-specific issues

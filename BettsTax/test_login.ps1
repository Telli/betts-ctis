# Test login endpoint for admin@bettsfirm.sl
$body = @{
    email = "admin@bettsfirm.sl"
    password = "Admin123!"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5001/api/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body `
        -ErrorAction Stop

    Write-Host "Login successful!" -ForegroundColor Green
    Write-Host "Token: $($response.token)"
    Write-Host "Roles: $($response.roles)"
} catch {
    Write-Host "Login failed!" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)"
    Write-Host "Error: $($_.ErrorDetails.Message)"
    
    # Test with default admin user
    Write-Host "`nTrying default admin user (admin@thebettsfirmsl.com / AdminPass123!)..." -ForegroundColor Yellow
    $body2 = @{
        email = "admin@thebettsfirmsl.com"
        password = "AdminPass123!"
    } | ConvertTo-Json
    
    try {
        $response2 = Invoke-RestMethod -Uri "http://localhost:5001/api/auth/login" `
            -Method Post `
            -ContentType "application/json" `
            -Body $body2
        Write-Host "Default admin login successful!" -ForegroundColor Green
    } catch {
        Write-Host "Default admin also failed" -ForegroundColor Red
    }
}

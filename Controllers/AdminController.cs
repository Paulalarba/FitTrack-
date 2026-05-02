using FitTrack.Data;
using FitTrack.Models;
using FitTrack.Services;
using FitTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    MemberQrCodeService qrCodeService,
    IWebHostEnvironment environment) : Controller
{
    private const int PageSize = 10;
    private const int ScanCooldownSeconds = 45;
    private static readonly HashSet<string> AllowedQrExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public async Task<IActionResult> Dashboard()
    {
        var model = new AdminDashboardViewModel
        {
            TotalUsers = await context.Users.CountAsync(),
            TotalTransactions = await context.Transactions.CountAsync(),
            ActiveMemberships = await context.Memberships.CountAsync(m => m.Status == "Active" && m.EndDate >= DateTime.UtcNow.Date),
            RecentUsers = await context.Users.OrderByDescending(u => u.JoinedDate).Take(5).ToListAsync(),
            RecentTransactions = await context.Transactions.Include(t => t.User).OrderByDescending(t => t.PaymentDate).Take(5).ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult ScanQR() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyQrCode([FromBody] QrScanRequest? request)
    {
        var now = DateTime.UtcNow;

        if (request is null || !ModelState.IsValid || !qrCodeService.TryReadPayload(request.QrPayload, out var payload))
        {
            await LogCheckInAsync(null, now, "Denied", "Invalid or tampered QR code");
            return Json(new
            {
                verified = false,
                allowEntry = false,
                status = "Denied",
                message = "Invalid or tampered QR code."
            });
        }

        var qrPayload = payload!;
        var member = await context.Users.FirstOrDefaultAsync(u => u.Id == qrPayload.MemberId);
        if (member is null || string.IsNullOrWhiteSpace(member.QrCodeToken) || member.QrCodeToken != qrPayload.Token)
        {
            await LogCheckInAsync(member?.Id, now, "Denied", "QR token is invalid or has been regenerated");
            return Json(new
            {
                verified = false,
                allowEntry = false,
                status = "Denied",
                message = "This QR code is no longer valid."
            });
        }

        var lastCheckIn = await context.CheckInLogs
            .Where(c => c.MemberId == member.Id)
            .OrderByDescending(c => c.CheckInTime)
            .FirstOrDefaultAsync();

        if (lastCheckIn is not null && lastCheckIn.CheckInTime >= now.AddSeconds(-ScanCooldownSeconds))
        {
            return Json(new
            {
                verified = true,
                allowEntry = lastCheckIn.Status == "Allowed",
                status = "Duplicate",
                message = $"Already scanned. Please wait {ScanCooldownSeconds} seconds between scans.",
                member = BuildMemberScanResponse(member),
                membership = await BuildMembershipScanResponseAsync(member.Id),
                checkInTime = lastCheckIn.CheckInTime
            });
        }

        var membership = await context.Memberships
            .Where(m => m.UserId == member.Id)
            .OrderByDescending(m => m.EndDate)
            .FirstOrDefaultAsync();

        var allowEntry = membership?.IsActive == true;
        var status = allowEntry ? "Allowed" : "Denied";
        var reason = allowEntry ? "Membership active" : "Membership expired or inactive";

        var log = new CheckInLog
        {
            MemberId = member.Id,
            CheckInTime = now,
            Status = status,
            Reason = reason
        };

        context.CheckInLogs.Add(log);
        await context.SaveChangesAsync();

        return Json(new
        {
            verified = true,
            allowEntry,
            status,
            message = allowEntry ? $"Welcome, {member.FullName}." : "Membership expired or inactive. Entry denied.",
            member = BuildMemberScanResponse(member),
            membership = BuildMembershipScanResponse(membership),
            checkInTime = log.CheckInTime,
            previousCheckInTime = lastCheckIn?.CheckInTime
        });
    }

    public async Task<IActionResult> Users(string? searchTerm, int page = 1)
    {
        var query = context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u =>
                u.FullName.Contains(searchTerm) ||
                (u.Email != null && u.Email.Contains(searchTerm)) ||
                u.Id.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.JoinedDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(u => new AdminUserListItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = u.Role,
                JoinedDate = u.JoinedDate,
                MembershipStatus = context.Memberships
                    .Where(m => m.UserId == u.Id)
                    .OrderByDescending(m => m.EndDate)
                    .Select(m => m.Status)
                    .FirstOrDefault() ?? "No membership"
            })
            .ToListAsync();

        return View(new AdminUsersViewModel
        {
            Users = users,
            SearchTerm = searchTerm,
            Page = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize)
        });
    }

    [HttpGet]
    public IActionResult CreateUser() => View(new ManageUserViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(ManageUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Password is required when creating a user.");
            return View(model);
        }

        if (!await roleManager.RoleExistsAsync(model.Role))
        {
            await roleManager.CreateAsync(new IdentityRole(model.Role));
        }

        var user = new ApplicationUser
        {
            FullName = model.FullName,
            Email = model.Email,
            UserName = model.Email,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            Role = model.Role,
            QrCodeToken = MemberQrCodeService.GenerateToken(),
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await userManager.AddToRoleAsync(user, model.Role);
        TempData["StatusMessage"] = "User account created.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(new ManageUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Role = user.Role
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(ManageUserViewModel model)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Id))
        {
            return View(model);
        }

        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;
        user.Role = model.Role;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var existingRoles = await userManager.GetRolesAsync(user);
        foreach (var existingRole in existingRoles)
        {
            await userManager.RemoveFromRoleAsync(user, existingRole);
        }

        if (!await roleManager.RoleExistsAsync(model.Role))
        {
            await roleManager.CreateAsync(new IdentityRole(model.Role));
        }

        await userManager.AddToRoleAsync(user, model.Role);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await userManager.ResetPasswordAsync(user, resetToken, model.Password);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
        }

        TempData["StatusMessage"] = "User account updated.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Email?.Equals("admin@fittrack.local", StringComparison.OrdinalIgnoreCase) == true)
        {
            TempData["StatusMessage"] = "The seeded administrator account cannot be deleted.";
            return RedirectToAction(nameof(Users));
        }

        var memberships = await context.Memberships.Where(m => m.UserId == id).ToListAsync();
        var transactions = await context.Transactions.Where(t => t.UserId == id).ToListAsync();
        context.Memberships.RemoveRange(memberships);
        context.Transactions.RemoveRange(transactions);
        await context.SaveChangesAsync();

        await userManager.DeleteAsync(user);
        TempData["StatusMessage"] = "User account deleted.";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Transactions(string? searchTerm, string? statusFilter, int page = 1)
    {
        var query = context.Transactions.Include(t => t.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t =>
                t.MembershipPlan.Contains(searchTerm) ||
                t.Id.ToString().Contains(searchTerm) ||
                (t.User != null && (t.User.FullName.Contains(searchTerm) || (t.User.Email != null && t.User.Email.Contains(searchTerm)))));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(t => t.Status == statusFilter);
        }

        var totalCount = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.PaymentDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return View(new AdminTransactionsViewModel
        {
            Transactions = transactions,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            Page = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize)
        });
    }

    public async Task<IActionResult> WalletRequests()
    {
        var requests = await context.WalletTransactions
            .Include(t => t.Wallet)
            .ThenInclude(w => w!.User)
            .Where(t => t.Type == "Credit" && t.Status == "Pending")
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveDeposit(int id)
    {
        var request = await context.WalletTransactions
            .Include(t => t.Wallet)
            .FirstOrDefaultAsync(t => t.Id == id && t.Type == "Credit");

        if (request is null || request.Wallet is null)
        {
            return NotFound();
        }

        if (request.Status != "Pending")
        {
            TempData["StatusMessage"] = $"Deposit request #{request.Id} has already been {request.Status.ToLower()}.";
            return RedirectToAction(nameof(WalletRequests));
        }

        request.Status = "Approved";
        request.Wallet.Balance += request.Amount;

        await context.SaveChangesAsync();
        TempData["StatusMessage"] = $"Deposit request #{request.Id} approved.";
        return RedirectToAction(nameof(WalletRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectDeposit(int id)
    {
        var request = await context.WalletTransactions
            .FirstOrDefaultAsync(t => t.Id == id && t.Type == "Credit");

        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != "Pending")
        {
            TempData["StatusMessage"] = $"Deposit request #{request.Id} has already been {request.Status.ToLower()}.";
            return RedirectToAction(nameof(WalletRequests));
        }

        request.Status = "Rejected";

        await context.SaveChangesAsync();
        TempData["StatusMessage"] = $"Deposit request #{request.Id} rejected.";
        return RedirectToAction(nameof(WalletRequests));
    }

    public async Task<IActionResult> PaymentAccounts()
    {
        var accounts = await context.PaymentAccounts
            .OrderBy(p => p.PaymentType)
            .ThenBy(p => p.AccountName)
            .ToListAsync();

        return View(accounts);
    }

    [HttpGet]
    public IActionResult CreatePaymentAccount() => View(new PaymentAccountViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePaymentAccount(PaymentAccountViewModel model)
    {
        ValidateQrCode(model.QrCodeFile, requireFile: true);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var qrCodePath = await SaveQrCodeAsync(model.QrCodeFile!);
        context.PaymentAccounts.Add(new PaymentAccount
        {
            AccountName = model.AccountName,
            AccountNumber = model.AccountNumber,
            PaymentType = model.PaymentType,
            QrCodePath = qrCodePath,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Payment account added.";
        return RedirectToAction(nameof(PaymentAccounts));
    }

    [HttpGet]
    public async Task<IActionResult> EditPaymentAccount(int id)
    {
        var account = await context.PaymentAccounts.FindAsync(id);
        if (account is null)
        {
            return NotFound();
        }

        return View(new PaymentAccountViewModel
        {
            Id = account.Id,
            AccountName = account.AccountName,
            AccountNumber = account.AccountNumber,
            PaymentType = account.PaymentType,
            ExistingQrCodePath = account.QrCodePath
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPaymentAccount(PaymentAccountViewModel model)
    {
        if (model.Id is null)
        {
            return NotFound();
        }

        var account = await context.PaymentAccounts.FindAsync(model.Id.Value);
        if (account is null)
        {
            return NotFound();
        }

        ValidateQrCode(model.QrCodeFile, requireFile: false);

        if (!ModelState.IsValid)
        {
            model.ExistingQrCodePath = account.QrCodePath;
            return View(model);
        }

        account.AccountName = model.AccountName;
        account.AccountNumber = model.AccountNumber;
        account.PaymentType = model.PaymentType;
        account.UpdatedAt = DateTime.UtcNow;

        if (model.QrCodeFile is not null)
        {
            account.QrCodePath = await SaveQrCodeAsync(model.QrCodeFile);
        }

        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Payment account updated.";
        return RedirectToAction(nameof(PaymentAccounts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePaymentAccount(int id)
    {
        var account = await context.PaymentAccounts.FindAsync(id);
        if (account is null)
        {
            return NotFound();
        }

        context.PaymentAccounts.Remove(account);
        await context.SaveChangesAsync();

        TempData["StatusMessage"] = "Payment account removed.";
        return RedirectToAction(nameof(PaymentAccounts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTransactionStatus(int id, string status)
    {
        var transaction = await context.Transactions.FirstOrDefaultAsync(t => t.Id == id);
        if (transaction is null)
        {
            return NotFound();
        }

        transaction.Status = status;

        if (status == "Approved")
        {
            var startDate = DateTime.UtcNow.Date;
            var endDate = transaction.MembershipPlan.Equals("Annual", StringComparison.OrdinalIgnoreCase)
                ? startDate.AddYears(1)
                : startDate.AddMonths(1);

            var membership = await context.Memberships
                .Where(m => m.UserId == transaction.UserId)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            if (membership is null)
            {
                membership = new Membership
                {
                    UserId = transaction.UserId,
                    PlanName = transaction.MembershipPlan,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "Active",
                    MonthlyFee = transaction.Amount
                };
                context.Memberships.Add(membership);
            }
            else
            {
                membership.PlanName = transaction.MembershipPlan;
                membership.StartDate = startDate;
                membership.EndDate = endDate;
                membership.Status = "Active";
                membership.MonthlyFee = transaction.Amount;
            }
        }
        else if (status == "Rejected")
        {
            var membership = await context.Memberships
                .Where(m => m.UserId == transaction.UserId)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            if (membership is not null && membership.Status == "Pending")
            {
                membership.Status = "Rejected";
            }
        }

        await context.SaveChangesAsync();
        TempData["StatusMessage"] = $"Transaction #{transaction.Id} marked as {status}.";
        return RedirectToAction(nameof(Transactions));
    }

    private void ValidateQrCode(IFormFile? file, bool requireFile)
    {
        if (file is null)
        {
            if (requireFile)
            {
                ModelState.AddModelError(nameof(PaymentAccountViewModel.QrCodeFile), "QR code image is required.");
            }

            return;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedQrExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(PaymentAccountViewModel.QrCodeFile), "QR code must be a JPG, PNG, or WEBP image.");
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(PaymentAccountViewModel.QrCodeFile), "QR code image must be 5 MB or smaller.");
        }
    }

    private async Task<string> SaveQrCodeAsync(IFormFile file)
    {
        var uploadsPath = Path.Combine(environment.WebRootPath, "uploads", "payment-accounts");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(uploadsPath, fileName);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        return $"/uploads/payment-accounts/{fileName}";
    }

    private async Task LogCheckInAsync(string? memberId, DateTime checkInTime, string status, string reason)
    {
        context.CheckInLogs.Add(new CheckInLog
        {
            MemberId = memberId,
            CheckInTime = checkInTime,
            Status = status,
            Reason = reason
        });

        await context.SaveChangesAsync();
    }

    private static object BuildMemberScanResponse(ApplicationUser member)
    {
        return new
        {
            id = member.Id,
            fullName = member.FullName,
            email = member.Email,
            profilePicture = member.ProfilePicture
        };
    }

    private async Task<object?> BuildMembershipScanResponseAsync(string memberId)
    {
        var membership = await context.Memberships
            .Where(m => m.UserId == memberId)
            .OrderByDescending(m => m.EndDate)
            .FirstOrDefaultAsync();

        return BuildMembershipScanResponse(membership);
    }

    private static object? BuildMembershipScanResponse(Membership? membership)
    {
        if (membership is null)
        {
            return null;
        }

        return new
        {
            planName = membership.PlanName,
            status = membership.IsActive ? "Active" : "Expired",
            endDate = membership.EndDate
        };
    }
}

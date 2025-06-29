using Microsoft.AspNetCore.Identity;
using LearnSphere.Models;

namespace LearnSphere.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, 
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager)
        {
            // Create roles
            string[] roleNames = { "Admin", "Student" };
            IdentityResult roleResult;

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create Admin user
            var adminUser = await userManager.FindByEmailAsync("admin@learnsphere.com");
            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@learnsphere.com",
                    Email = "admin@learnsphere.com",
                    FullName = "System Administrator",
                    Role = "Admin",
                    RegistrationDate = DateTime.UtcNow,
                    EmailConfirmed = true,
                    IsActive = true,
                    Department = "IT Administration"
                };

                var createAdminResult = await userManager.CreateAsync(admin, "Admin123!");
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // Create Student users
            var student1 = await userManager.FindByEmailAsync("john.doe@student.com");
            if (student1 == null)
            {
                var johnDoe = new ApplicationUser
                {
                    UserName = "john.doe@student.com",
                    Email = "john.doe@student.com",
                    FullName = "John Doe",
                    Role = "Student",
                    RegistrationDate = DateTime.UtcNow,
                    EmailConfirmed = true,
                    IsActive = true,
                    Department = "Computer Science",
                    Bio = "Enthusiastic computer science student interested in web development and AI."
                };

                var createStudentResult = await userManager.CreateAsync(johnDoe, "Student123!");
                if (createStudentResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(johnDoe, "Student");
                }
            }

            var student2 = await userManager.FindByEmailAsync("jane.smith@student.com");
            if (student2 == null)
            {
                var janeSmith = new ApplicationUser
                {
                    UserName = "jane.smith@student.com",
                    Email = "jane.smith@student.com",
                    FullName = "Jane Smith",
                    Role = "Student",
                    RegistrationDate = DateTime.UtcNow,
                    EmailConfirmed = true,
                    IsActive = true,
                    Department = "Data Science",
                    Bio = "Data science student passionate about machine learning and analytics."
                };

                var createStudentResult2 = await userManager.CreateAsync(janeSmith, "Student123!");
                if (createStudentResult2.Succeeded)
                {
                    await userManager.AddToRoleAsync(janeSmith, "Student");
                }
            }

            // Seed sample learning resources
            using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            
            if (!context.LearningResources.Any())
            {
                var admin = await userManager.FindByEmailAsync("admin@learnsphere.com");
                if (admin != null)
                {
                    var sampleResources = new List<LearningResource>
                    {
                        new LearningResource
                        {
                            Title = "Introduction to ASP.NET Core",
                            Description = "Learn the fundamentals of ASP.NET Core development, including MVC pattern, dependency injection, and Entity Framework.",
                            ResourceType = ResourceTypes.Document,
                            Category = "Web Development",
                            Tags = "asp.net, mvc, web development",
                            UploadedById = admin.Id,
                            UploadDate = DateTime.UtcNow
                        },
                        new LearningResource
                        {
                            Title = "Database Design Principles",
                            Description = "Comprehensive guide to database design, normalization, and Entity Framework Core implementation.",
                            ResourceType = ResourceTypes.PDF,
                            Category = "Database",
                            Tags = "database, sql, entity framework",
                            UploadedById = admin.Id,
                            UploadDate = DateTime.UtcNow
                        },
                        new LearningResource
                        {
                            Title = "JavaScript Fundamentals",
                            Description = "Master JavaScript basics including variables, functions, objects, and modern ES6+ features.",
                            ResourceType = ResourceTypes.Video,
                            Category = "Programming",
                            Tags = "javascript, programming, frontend",
                            UploadedById = admin.Id,
                            UploadDate = DateTime.UtcNow
                        },
                        new LearningResource
                        {
                            Title = "C# Programming Quiz",
                            Description = "Test your knowledge of C# programming concepts including OOP, LINQ, and async programming.",
                            ResourceType = ResourceTypes.Quiz,
                            Category = "Programming",
                            Tags = "csharp, quiz, programming",
                            UploadedById = admin.Id,
                            UploadDate = DateTime.UtcNow
                        }
                    };

                    context.LearningResources.AddRange(sampleResources);
                    await context.SaveChangesAsync();

                    // Add sample quiz for the C# Programming resource
                    var quizResource = sampleResources.First(r => r.Title == "C# Programming Quiz");
                    var quiz = new Quiz
                    {
                        Title = "C# Fundamentals Assessment",
                        Description = "Test your understanding of C# programming fundamentals",
                        ResourceId = quizResource.Id,
                        TimeLimitMinutes = 30,
                        PassingScore = 70,
                        MaxAttempts = 3
                    };

                    context.Quizzes.Add(quiz);
                    await context.SaveChangesAsync();

                    // Add sample questions
                    var questions = new List<Question>
                    {
                        new Question
                        {
                            QuizId = quiz.Id,
                            QuestionText = "What is the correct way to declare a string variable in C#?",
                            OptionA = "string myString;",
                            OptionB = "String myString;",
                            OptionC = "var myString = '';",
                            OptionD = "All of the above",
                            CorrectAnswer = "D",
                            Explanation = "All options are valid ways to declare a string in C#.",
                            OrderIndex = 1,
                            Points = 1
                        },
                        new Question
                        {
                            QuizId = quiz.Id,
                            QuestionText = "Which access modifier makes a member accessible only within the same class?",
                            OptionA = "public",
                            OptionB = "private",
                            OptionC = "protected",
                            OptionD = "internal",
                            CorrectAnswer = "B",
                            Explanation = "Private members are only accessible within the same class.",
                            OrderIndex = 2,
                            Points = 1
                        },
                        new Question
                        {
                            QuizId = quiz.Id,
                            QuestionText = "What does LINQ stand for?",
                            OptionA = "Language Integrated Query",
                            OptionB = "Linear Integrated Query",
                            OptionC = "Language Independent Query",
                            OptionD = "Linear Independent Query",
                            CorrectAnswer = "A",
                            Explanation = "LINQ stands for Language Integrated Query.",
                            OrderIndex = 3,
                            Points = 1
                        }
                    };

                    context.Questions.AddRange(questions);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}

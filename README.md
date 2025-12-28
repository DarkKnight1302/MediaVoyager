# MediaVoyager Backend

<div align="center">

![C#](https://img.shields.io/badge/C%23-100%25-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Azure](https://img.shields.io/badge/Azure-0078D4?style=for-the-badge&logo=microsoft-azure&logoColor=white)
![Google Gemini](https://img.shields.io/badge/Google%20Gemini-4285F4?style=for-the-badge&logo=google&logoColor=white)

**AI-Powered Movies and TV Series Recommendation Engine**

🌐 **Live Application:** [https://www.mediavoyager.in/](https://www.mediavoyager.in/)

</div>

---

## 📖 Overview

MediaVoyager is an intelligent recommendation engine that provides personalized movie and TV show suggestions using artificial intelligence. The system analyzes user preferences, favorite content, and watch history to deliver tailored recommendations that match individual tastes.

## ✨ Features

- **🤖 AI-Powered Recommendations** - Leverages Google Gemini AI to understand user taste and generate personalized recommendations
- **🎬 Movie Recommendations** - Get movie suggestions based on your favorite films
- **📺 TV Show Recommendations** - Discover new TV series tailored to your preferences
- **👤 User Management** - Secure user authentication with Google Sign-In support
- **📋 Watchlist Management** - Maintain separate watchlists for movies and TV shows
- **📊 Watch History Tracking** - Track what you've watched to avoid duplicate recommendations
- **⭐ IMDb Ratings** - Displays IMDb ratings for movies
- **⚡ Smart Caching** - Efficient caching system for movies and TV shows data
- **🔒 Secure Authentication** - JWT-based authentication and authorization
- **🚦 Rate Limiting** - Built-in rate limiting to ensure service stability

## 🏗️ Architecture

### Tech Stack

| Component | Technology |
|-----------|------------|
| **Framework** | ASP.NET Core |
| **Language** | C# |
| **AI Engine** | Google Gemini 2.5 Flash |
| **Movie Database** | TMDb (The Movie Database) |
| **Ratings** | IMDb (via OMDb API) |
| **Cloud Platform** | Azure Container Apps |
| **Database** | Azure Cosmos DB |

### Project Structure

```
MediaVoyager/
├── Clients/                    # External API clients
│   └── GeminiRecommendationClient.cs
├── Controllers/                # API Controllers
├── Entities/                   # Data entities
├── Handlers/                   # Request handlers
│   └── SignInHandler.cs
├── Middleware/                 # Custom middleware
├── Models/                     # Request/Response models
├── Repositories/               # Data access layer
│   ├── UserRepository.cs
│   ├── UserMoviesRepository.cs
│   ├── UserTvRepository.cs
│   ├── MovieCacheRepository.cs
│   └── TvShowCacheRepository. cs
├── Services/                   # Business logic
│   ├── MediaRecommendationService.cs
│   ├── UserMediaService.cs
│   └── TmdbCacheService.cs
└── Program.cs                  # Application entry point
```

## 🧠 How AI Recommendations Work

MediaVoyager uses Google's Gemini AI to provide intelligent recommendations:

1. **Taste Analysis** - The AI analyzes your list of favorite movies/TV shows to understand your preferences
2. **Context Awareness** - Watch history is considered to avoid recommending content you've already seen
3. **Personalized Output** - The AI generates a single, highly relevant recommendation based on your unique taste profile
4. **Adaptive Temperature** - If initial recommendations don't yield results, the system retries with adjusted parameters for better results

```
User Favorites → Gemini AI Analysis → Taste Understanding → Personalized Recommendation
```

## 🔧 Key Services

### MediaRecommendationService
The core service that orchestrates the recommendation flow:
- Fetches user preferences and watch history
- Communicates with the Gemini AI client
- Validates and processes AI responses
- Returns enriched movie/TV show data from TMDb

### GeminiRecommendationClient
Handles all communication with Google's Gemini API:
- Rate-limited to ensure API compliance
- Supports configurable temperature for response variation
- Provides separate recommendation logic for movies and TV shows

### TMDb Integration
Integrated with The Movie Database (TMDb) for rich media information:
- Movie details, posters, and metadata
- TV show information and episode details
- Cast and crew information
- Trailers and videos

## 🚀 Deployment

The application is deployed on **Azure Container Apps** using source-based deployment with buildpacks:

- **Health Endpoint:** `/health`
- **Graceful Shutdown:** 30-second timeout
- **Logging:** Console logging optimized for containers

For detailed deployment instructions, refer to [DEPLOYMENT.md](DEPLOYMENT. md).

## 🔐 Environment Variables

The application requires the following secrets (managed via Azure Key Vault or similar):

| Secret | Description |
|--------|-------------|
| `gemini_api_key` | Google Gemini API key for AI recommendations |
| `groq_api_key` | Groq API key for AI recommendations (Chat Completions) |
| `tmdb_auth` | TMDb API authentication token |

## 📦 Dependencies

- **NewHorizonLib** - Shared library for common services
- **TMDbLib** - . NET wrapper for TMDb API
- **Newtonsoft.Json** - JSON serialization

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is open source and available under the [MIT License](LICENSE).

---

<div align="center">

**Built with ❤️ using .NET and AI**

</div>

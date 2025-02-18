import './App.css'
import { UserProvider } from "./Context/useAuth.tsx";
import { BrowserRouter, Route, Routes } from "react-router";
import { Toaster } from "react-hot-toast";
import { Home } from "./Components/Home/Home.tsx";
import { LoginPage } from "./Components/LoginPage/LoginPage.tsx";
import { RegisterPage } from "./Components/RegisterPage/RegisterPage.tsx";
import { Navbar } from "./Components/Navbar/Navbar.tsx";
import { Footer } from "./Components/Footer/Footer.tsx";
import { ForumPage } from "./Components/ForumPage/ForumPage.tsx";
import CreateEstate from './Components/CreateEstate/CreateEstate.tsx';
import { PostPage } from "./Components/PostPage/PostPage.tsx";
import "leaflet/dist/leaflet.css";
import { UserProfile } from './Components/UserProfile/UserProfile.tsx';
import SearchEstate from './Components/SearchEstate/SearchEstate.tsx';
import { EstatePage } from './Components/EstatePage/EstatePage.tsx';
import { ProtectedRoute } from "./Components/ProtectedRoute/ProtectedRoute.tsx";
import { FavoriteEstates } from './Components/FavoriteEstatesPage/FavoriteEstatesPage.tsx';

function App() {

  return (
    <>
      <BrowserRouter>
        <UserProvider>
          <div className="App">
            <Navbar />
            <div className="content">
              <Routes>
                <Route path="/" element={<Home />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/forum" element={<ProtectedRoute><ForumPage /></ProtectedRoute>} />
                <Route path="/forum/:postId" element={<ProtectedRoute><PostPage /></ProtectedRoute>} />
                <Route path="/create-estate" element={<ProtectedRoute><CreateEstate /></ProtectedRoute>} />
                <Route path="/user-profile/:id" element={<ProtectedRoute><UserProfile /></ProtectedRoute>} />
                <Route path="/search-estates" element={<SearchEstate />} />
                <Route path="/estate-page/:id" element={<ProtectedRoute><EstatePage /></ProtectedRoute>} />
                <Route path="/estate-details/:id" element={<ProtectedRoute><EstatePage /></ProtectedRoute>} />
                <Route path="/favorite-estates" element={<ProtectedRoute><FavoriteEstates /></ProtectedRoute>} />
              </Routes>
            </div>
            <Footer />
            <Toaster position='top-center' reverseOrder={false} />
          </div>
        </UserProvider>
      </BrowserRouter>
    </>
  )
}

export default App

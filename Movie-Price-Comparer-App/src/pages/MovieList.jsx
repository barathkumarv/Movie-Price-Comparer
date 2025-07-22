import React, { useEffect, useState } from 'react';
import axios from 'axios';
import Spinner from '../components/Spinner';
import MovieCard from '../components/MovieCard';

const MovieList = () => {
  const [movies, setMovies] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    axios.get('/api/Movies')
      .then(res => {
        if (res.data.success) {
          setMovies(res.data.data);
        }
      })
      .catch(err => console.error(err))
      .finally(() => setLoading(false));
  }, []);

  return (
    <>
      <h2 className="header">🎬 Movie Price Comparisons</h2>
      <div className="movie-grid">
        {loading ? <Spinner /> : movies.map(movie => (
          <MovieCard key={movie.movieId} movie={movie} />
        ))}
      </div>
    </>
  );
};

export default MovieList;

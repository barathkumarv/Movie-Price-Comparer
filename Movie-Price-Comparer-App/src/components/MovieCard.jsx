import React from 'react';

const MovieCard = ({ movie }) => {
  const { title, year, poster, providerPrices, cheapestProvider, cheapestPrice } = movie;

  return (
    <div className="card">
      <img src={poster} alt={title} />
      <div className="card-body">
        <h5 className="card-title">{title} ({year})</h5>
        {Object.entries(providerPrices).map(([provider, price]) => (
          <div key={provider} className="card-pricing">
            {provider}: ${price.toFixed(2)}
          </div>
        ))}
        <div className="card-cheapest">
          💸 Cheapest: {cheapestProvider} @ ${cheapestPrice.toFixed(2)}
        </div>
      </div>
    </div>
  );
};

export default MovieCard;

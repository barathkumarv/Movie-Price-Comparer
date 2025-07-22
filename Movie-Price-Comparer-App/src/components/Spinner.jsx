import React from 'react';
import { ClipLoader } from 'react-spinners';

const Spinner = () => (
  <div className="spinner-overlay">
    <ClipLoader size={50} color="#ffffff" />
    <p>Loading best deals... Please hang on</p>
  </div>
);

export default Spinner;

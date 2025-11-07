import React from 'react';
import ReactDOM from 'react-dom';
import './style.css';

function App() {
    return (
        <div>
            <h2>Session: Speech Progress</h2>
            <button onClick={fetchData}>Get Session Data</button>
            <div id="output"></div>
        </div>
    );
}

function fetchData() {
    fetch('http://localhost:8081/api/analysis/pronunciation', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text: "hello world" })
    })
    .then(res => res.json())
    .then(data => {
        document.getElementById('output').innerText = JSON.stringify(data, null, 2);
    })
    .catch(error => {
        console.error("Error fetching data:", error);
    });
}

ReactDOM.render(<App />, document.getElementById('app'));
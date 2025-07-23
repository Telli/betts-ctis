#!/bin/bash

# Start servers for full-stack E2E testing

set -e

echo "🚀 Starting Backend Server..."
cd ../BettsTax/BettsTax.Web
dotnet build --no-restore
if [ $? -eq 0 ]; then
    echo "✅ Backend build successful"
    dotnet run --no-build &
    BACKEND_PID=$!
    echo "🔧 Backend server started with PID: $BACKEND_PID"
else
    echo "❌ Backend build failed"
    exit 1
fi

cd ../../sierra-leone-ctis

echo "🚀 Starting Frontend Server..."
npm run dev &
FRONTEND_PID=$!
echo "🔧 Frontend server started with PID: $FRONTEND_PID"

echo "⏳ Waiting for servers to be ready..."
sleep 15

echo "✅ Both servers should be running:"
echo "   - Frontend: http://localhost:3000"
echo "   - Backend: http://localhost:5000"

# Keep servers running
echo "Press Ctrl+C to stop both servers..."
trap "kill $BACKEND_PID $FRONTEND_PID" INT TERM
wait
%% cleans up workspace and closes any open graphs
clear;
close all;

%% Important Variables
%{
    This section should have everything you'd normally need to edit.
%}

rtDataFile = '/Users/Mingda/Desktop/analysis files/SampleData/VRMoveTrack/rtData-2023-11-08-14-Brandon.csv';
eyeTrackingDataFile = '/Users/Mingda/Desktop/analysis files/SampleData/VRMoveTrack/MovementTracking-110823-Brandon.csv';

msBeforeChange = 2000;
msAfterChange = 2000;
timeDelay = 300; % delay between recorded time of ball direction change and recorded eye tracking
eccSpeed = 10; % deg per second

splitStr = strsplit(rtDataFile, "/");
fileName = splitStr{end};
horizTitle = "Horizontal eye tracking for " + fileName;
horizYlabel = "Horizontal eccentricity (°)";
horizXlabel = "Time (s)";
vertTitle = "Vertical eye tracking for " + fileName;
vertYLabel = "Vertical eccentricity (°)";
vertXLabel = "Time (s)";

%% Eye tracking pre-processing
%{
    This section sets the array eyeTimes and eyePositions where
    eyePositions is an nx3 array with columns denoting the x, y, and z
    vectors for the eye tracking
%}
eyeTrackingData = readtable(eyeTrackingDataFile, 'Delimiter', ';');
eyePositionsCell = eyeTrackingData.CombinedGazeForward;

% Remove the parentheses and split the "CombinedGazeForward" column
eyePositionsCell = erase(eyePositionsCell, '(');
eyePositionsCell = erase(eyePositionsCell, ')');
eyePositions = zeros(numel(eyePositionsCell),3);
for i= 1:numel(eyePositionsCell)
    eyePositions(i, :) = str2double(strsplit(eyePositionsCell{i},','));
end
eyeTimes = eyeTrackingData.CaptureTime - timeDelay;

%% Reaction time pre-processing
%{
    This section sets the array closestTimesIndices where each row
    corresponds to a position change, with columns denoting the unix time
    before the change, the time of the position change, and the time after.
    The amount of time before and after are taken from the variables,
    "msBeforeChange" and "msAfterChange"
%}
rtData = readtable(rtDataFile);
rtDataChanges = rtData(rtData.DirectionChange == "True",:);
closestTimesIndices = zeros(numel(rtDataChanges.Time),3);

for i = 1:numel(rtDataChanges.Time)
    [~, timeBefore] = min(abs(eyeTimes - (rtDataChanges.Time(i) - msBeforeChange)));
    [~, timeOfChange] = min(abs(eyeTimes - rtDataChanges.Time(i)));
    [~, timeAfter] = min(abs(eyeTimes - (rtDataChanges.Time(i) + msAfterChange)));
    closestTimesIndices(i,1) = timeBefore;
    closestTimesIndices(i,2) = timeOfChange;
    closestTimesIndices(i,3) = timeAfter;
end

%% Plotting
%{
    This section uses the pre-processed/organized data to create graphs.
%}

% GRAPH 1 & 2
%{
    These graphs show the vertical and horizontal components of eye
    tracking for some amount of time before and after each direction change
%}

% sets up figure windows and creates axes
horizFig = figure(1);
horizAxes = gca();
hold(horizAxes, 'ON');
vertFig = figure(2);
vertAxes = gca();
hold(vertAxes, 'ON')

% calculate vectors and plot data
for i = 1:height(rtDataChanges) % change this value if you only want single trials
    gaze = eyePositions(closestTimesIndices(i,1):closestTimesIndices(i,3),:);
    horizontalGaze = zeros(height(gaze), 1);
    verticalGaze = zeros(height(gaze), 1);
    
    % separates eye tracking vector into only horizontal and vertical
    % components
    for j = 1:height(gaze)
        % Given gaze vector
        x = gaze(j, 1);
        y = gaze(j, 2);
        z = gaze(j, 3);
        
        % Compute azimuth and elevation in radians
        theta_rad = atan2(x, z);
        phi_rad = atan2(y, sqrt(x^2 + z^2));
        
        % Convert radians to degrees
        theta_deg = rad2deg(theta_rad);
        phi_deg = rad2deg(phi_rad);
    
        horizontalGaze(j) = theta_deg;
        verticalGaze(j) = phi_deg;
    end

    % checks against the c# calculation
    % ??? why is this always different
    horizontalGaze2 = eyeTrackingData.CalcXEccentricity(closestTimesIndices(i,1):closestTimesIndices(i,3), :);
    verticalGaze2 = eyeTrackingData.CalcYEccentricity(closestTimesIndices(i,1):closestTimesIndices(i,3), :);
    if (max(horizontalGaze - horizontalGaze2) >1.5 || max(verticalGaze - verticalGaze2) > 1.0)
        disp("high imprecision: " + max(horizontalGaze - horizontalGaze2) + "; " + max(verticalGaze - verticalGaze2));
    end

    % sets the gaze at the time of msBeforeChange to be 0 and also
    % reverses the values if the directionChange is negative
    horizontalGaze = abs(horizontalGaze - horizontalGaze(1));
    verticalGaze = abs(verticalGaze - verticalGaze(1));

    % sets the time of the direction change to be 0
    plotTimes = eyeTimes(closestTimesIndices(i,1):closestTimesIndices(i,3));
    plotTimes = plotTimes - eyeTimes(closestTimesIndices(i,2));
    plotTimes = plotTimes / 1000;
    
    % plots trial
    plot(horizAxes, plotTimes, horizontalGaze);
    plot(vertAxes, plotTimes, verticalGaze);

end

plot(horizAxes, [-(msBeforeChange/1000); 0], [0; (msBeforeChange/1000)* eccSpeed], 'k--');
plot(horizAxes, [0; (msAfterChange/1000)], [(msBeforeChange/1000)* eccSpeed; (msBeforeChange/1000)* eccSpeed-((msAfterChange/1000) * eccSpeed)], 'k--');

xlim(horizAxes, [-msBeforeChange, msAfterChange]/1000)
xlim(vertAxes, [-msBeforeChange, msAfterChange]/1000)
title(horizAxes, horizTitle);
ylabel(horizAxes, horizYlabel);
xlabel(horizAxes, horizXlabel);
title(vertAxes, vertTitle);
ylabel(vertAxes, vertYLabel);
xlabel(vertAxes, vertXLabel);

hold(horizAxes, 'OFF');
hold(vertAxes, 'OFF');
%% Statistics Tests
%{
    This section performs the statistics tests necessary. For now, it is
    empty since I haven't figured out how the tests work in our old
    analysis code.
%}
%% clear variables
%{
    This section is for cleaning up anything that needs to be cleaned up. I
    also delete many temporary variables so only the important ones are
    left when the program finishes running. If you're debugging and the
    code is broken, it's a good idea to comment out this entire section. If
    the code runs, but something looks off, it's a good idea to take a look
    at the variables left in the workspace to see if a file was imported
    wrong or was unexpected.
%}

% delete the right bracket below to comment out everything
%{}

% saves data to data.mat file in the current directory
%save("data.mat", "data");

% preprocessing variables
clear eyePositionsCell fileName i splitStr timeAfter timeBefore ...
    timeOfChange;
% graphing variables
clear gaze horizontalGaze horizontalGaze2 j phi_deg phi_rad plotTimes ...
    theta_deg theta_rad verticalGaze verticalGaze2 x y z;
% anova / stats variables
%}






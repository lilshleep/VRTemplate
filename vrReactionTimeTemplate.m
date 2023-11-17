%% cleans up workspace and closes any open graphs
clear;
close all;

%% Important Variables
%{
    This section should have everything you'd normally need to edit.
%}

folderPath = "/Users/Mingda/Desktop/analysis files/SampleData/vrScaleData";
timeDelay = 150;
plotTitle = "VR 2D Face Scaling RT";
plotXLabel = "Eccentricity (°)";
plotYLabel = "reaction time (ms)";
plotYLimits = [400,800];

% z* value for confidence interval
zScore = 1.96; % 1.96 for 95%, 2.326 for 98%, 2.576 for 99%
pValThreshold = 0.05; % adjust accordingly if changing z-score
leftIVRows = 1:3; % left best fit line calculated for these IV rows in stats/data{i,4}
rightIVRows = 3:4; % right best fit line calculated for these IV rows in  stats/data{i,4}

%% Data pre-processing
%{
    This section imports all of the .csv files from folderPath, organizes
    and then adds them to the cell array named "data".
    The organization of the "data" variable is explained thoroughly in the
    next section.
%}

% Imports all files in the folder, folderPath, and adds to "data"
fileList = dir(fullfile(folderPath, "*.csv"));
data = cell(length(fileList), 3);
for i = 1:length(fileList)
    dataImport = readtable(fullfile(folderPath, fileList(i).name));
    % sets the participant ID for each file (used in the legend during
    % graphing) to the first column of "data"
    splitStr = strsplit(fileList(i).name, "-");
    participantID = splitStr{1};
    data{i, 1} = participantID;
    % sets second and third columns of "data"
    data{i, 2} = fileList(i).name;
    data{i, 3} = dataImport;
end
% remove all incorrect trials and trials > 1.2s from "data", then subtracts
% the time delay
for i = 1:height(data)
    currentData = data{i, 3};
    % removes incorrect trials
    currentData = currentData(strcmp(currentData.Correct, "True"), :);
    % removes trials > 1200 ms
    currentData = currentData(((currentData.ReactionTime-currentData.ObjShowTime) < 1200),:);
    % subtracts time delay from "data" reaction time
    currentData.ReactionTime = currentData.ReactionTime-timeDelay;
    data{i, 4} = currentData;
end

% go through each data file to remove outliers from the fourth column and
% create the fifth column in "data"
for i = 1:height(data)
    % gets the trials from one file
    currentData = data{i, 4};

    % separates the table into arrays
    independentVar = currentData.Distance;
    uniqueIVs = unique(independentVar);
    dependentVar = currentData.ReactionTime - currentData.ObjShowTime;

    % instantiates the table dataSeparatedByIV
    colTypes = ["string","cell","double","double","double"];
    colNames = ["IndependentVar", "Data", "Mean", "Std Dev", "Std Err"];
    dataSeparatedByIV = table('Size',[numel(uniqueIVs),numel(colTypes)],...
        'VariableTypes',colTypes,'VariableNames',colNames);

    % goes through each IV for current data file
    for j = 1:numel(uniqueIVs)
        % sets current IV and gets all of the DVs for that IV
        iv = uniqueIVs(j);
        rtForCondition = dependentVar(strcmpi(independentVar, iv));
        % this variable is for removing outliers from the 4th column of
        % "data"
        conditionIndices = find(strcmpi(independentVar, iv));

        % removes outliers
        [rtForCondition, outlierIndex] = rmoutliers(rtForCondition, "quartiles");

        % Maps back to original indices and removes corresponding rows from
        % currentData in order to remove outliers from 4th column of "data"
        outlierIndices = conditionIndices(outlierIndex);

        % adds cleaned data and summary statistics to the table
        dataSeparatedByIV(j,1) = iv;
        dataSeparatedByIV{j,2} = {rtForCondition};
        dataSeparatedByIV{j,3} = mean(rtForCondition);
        dataSeparatedByIV{j,4} = std(rtForCondition);
        dataSeparatedByIV{j,5} = std(rtForCondition)/sqrt(numel(rtForCondition));
    end
    % saves info to data variable
    currentData(outlierIndices, :) = [];
    data{i, 4} = currentData; % removes outliers from 4th column of "data"
    data{i, 5} = dataSeparatedByIV;
end

%% Plotting
%{
    This section uses the pre-processed/organized data to create graphs.
%}
%{
    Here is an explanation of the organization of the "data" variable:

    Each row is a separate .csv file found in the folder (The "imported
    data" column is the exact table imported from the .csv file). ^All 
    trials in the "cleaned trials" column have incorrect, >1200 ms, and
    outliers removed and time delay subtracted.

    column 1                column 2            column 3            column 4                column 5
    Participant ID,         file name,          imported data,      cleaned trials^,        stats
    Mingda
    JoeBruin
    CElegans
    CatSushi
    ...
    
    stats are organized as follows, where each row is a different level
    of the independent variable (e.g. for the eccentricity experiment: 
    -30°, -15°, 0°, 15°, 30°). ^All trials here have incorrect, >1200 ms,
    and outliers removed and time delay subtracted.

    column 1        column 2        column 3    column 4                column 5
    IV name,        Trials^,        Mean,       Standard Deviation,     Standard Error
    IV1
    IV2
    IV3
    ...
%}

% EXAMPLES ----------
% EXAMPLE 1: to plot a histogram of all of the data from a single participant
%{
i = 1;
currentData = data{i,4};
histogram(currentData.ReactionTime-currentData.ObjShowTime);
%}

% EXAMPLE 2: to plot a histogram for a single IV for a single participant
%{
i = 1;
currentData = data{i,5}; % gets stats for participant i = 1
j = 2;
% method 1
currentIVDataCell = currentData{j,2}; % gets trials for IV j = 1
% currentIVDataCell is an array in a cell, so to get it out we use:
currentIVData = currentIVDataCell{1};
histogram(currentIVData)
% method 2
currentIVData= currentData.Data{j}; % gets trials for IV j = 1
histogram(currentIVData)
%}

% EXAMPLE 3: to plot means and standard dev for a participant
%{
i = 1;
currentData = data{i,5}; % gets stats for participant i = 1
% method 1
figure();
hold on;
for j = 1:height(currentData)
    mean = currentData{j,3};
    standardError = currentData{j,5};
    errorbar(j, mean, standardError*zScore); % errorbar for 95% Conf interval
    scatter(j, mean);
end
hold off;
% method 2
figure();
hold on;
xVals = 1:height(currentData);
errorbar(xVals, currentData.Mean, currentData.("Std Err")*zScore); % errorbar for 95% Conf interval
scatter(xVals, currentData.Mean);
hold off;
%}
% EXAMPLES END -------


% creates colors for each participant
colors = lines(length(fileList));

% GRAPH 1
%{
    This graph shows each participant's mean and 95CI, with IVs on the
    x-axis and reaction time on the y-axis
%}

% sets up R^2 array
r2Values = zeros(height(data), 2);
% sets up figure window and creates an axis
f1 = figure(1);
ax1 = gca;
hold(ax1,"on");
% set from variables in first section
title(ax1, plotTitle);
xlabel(ax1, plotXLabel);
ylabel(ax1, plotYLabel);
ylim(ax1, plotYLimits);
% other plot aesthetics
plotXVals = 1:height(data{1,5}); % sets the x values for the plot using the first file's IVs
xlim(ax1, [min(plotXVals)-1, max(plotXVals)+1]);
customXTickLabels = data{1,5}.IndependentVar; % sets the x labels for the plot using the first file's IV names
xticks(ax1, plotXVals);
xticklabels(ax1, customXTickLabels);
legend(ax1);

% goes through each data file
for i = 1:height(data)
    % gets the data from one file
    currentData = data{i,5};
    % sets color of the graph
    currentColor = colors(i, :);

    % plot means for each IV
    nameForLegend = data{i,1};
    scatter(ax1, plotXVals, currentData.Mean, "filled", "MarkerFaceColor", currentColor, "DisplayName", nameForLegend);
    % plots error bar (95 CI)
    errorbar(ax1, plotXVals, currentData.Mean, currentData.("Std Err")*zScore, "Color", currentColor, "LineStyle", "none", "HandleVisibility", "off");

    % calculates left line of best fit
    % gets x and y of means for current data file
    leftYData = currentData{leftIVRows,3};
    leftXData = leftIVRows;
    % calculate line of best fit using least square regression
    [fitLineLeftCoefs] = polyfit(leftXData, leftYData, 1);
    fitLineLeft = polyval(fitLineLeftCoefs, leftXData);
    % calculate R^2 for left fit line
    yObservedLeft = leftYData.';
    yPredictedLeft = fitLineLeft;
    residualsLeft = yObservedLeft - yPredictedLeft; % calculate residuals
    sseLeft = sum(residualsLeft.^2); % calculate sum of squared errors
    sstLeft = sum((yObservedLeft - mean(yObservedLeft)).^2); % calculate total sum of squares
    r2Left = 1 - (sseLeft/sstLeft); % calculate R^2 value
    r2Values(i, 1) = r2Left; % store R^2 value

    % calculates right line of best fit
    % gets x and y of means for current data file
    rightYData = currentData{rightIVRows,3};
    rightXData = rightIVRows;
    % calculate line of best fit using least square regression
    [fitLineRightCoefs] = polyfit(rightXData, rightYData, 1);
    fitLineRight = polyval(fitLineRightCoefs, rightXData);
    % calculate R^2 for right fit line
    yObservedRight = rightYData.';
    yPredictedRight = fitLineRight;
    residualsRight = yObservedRight - yPredictedRight; % calculate residuals
    sseRight = sum(residualsRight.^2); % calculate sum of squared errors
    sstRight = sum((yObservedRight - mean(yObservedRight)).^2); % calculate total sum of squares
    r2Right = 1 - (sseRight/sstRight); % calculate R^2 value
    r2Values(i, 2) = r2Right; % store R^2 value
    
    % plot fit lines
    plot(ax1, leftXData, fitLineLeft, "Color", currentColor, "HandleVisibility", "off");
    plot(ax1, rightXData, fitLineRight,  "Color", currentColor, "HandleVisibility", "off");

    % prints R^2 values
    disp("ParticipantID: " + nameForLegend + "; Left r^2: " + r2Values(i,1) + "; Right r^2: " + r2Values(i,2));
end
hold(ax1,'off');




%% Statistics Tests
%{
    This section performs the statistics tests necessary. For now, it is
    empty since I haven't figured out how the tests work in our old
    analysis code.
%}
% Repeated Measures ANOVA test
% to-do, check for:
% Sphericity: This assumes that the variances of the differences between all possible pairs of within-subject conditions are equal. It is similar to the assumption of homogeneity of variance in a between-subjects ANOVA. Violations of sphericity can lead to an increased chance of Type I errors. The Mauchly’s test is commonly used to test for sphericity, and if this assumption is violated, corrections like Greenhouse-Geisser or Huynh-Feldt are applied to adjust the degrees of freedom.
% Normality: The distribution of the residuals (differences between observed and predicted values by the model) should be approximately normally distributed. This can be checked using plots (like Q-Q plots) or tests for normality (like the Shapiro-Wilk test). However, repeated measures ANOVA is considered robust to violations of this assumption, especially with larger sample sizes.

% One way ANOVA test
% performs a one way anova for each subject
%{
anovaGraphs = "off"; % display stats / anova graphs
for i = 1:height(data)
    currentData = data{i,5};
    currentIVs = [];
    anovaArray = [];
    for j = 1:height(currentData)
        currentIVs = [currentIVs; repmat(currentData.IndependentVar(j), length(currentData.Data{j}), 1);];
        anovaArray = [anovaArray; currentData.Data{j}];
    end
    [p, tbl, stats] = anova1(anovaArray, currentIVs, anovaGraphs);

    % Perform multiple comparisons using multcompare
    [c, m, h, groupNames] = multcompare(stats, 'Display', anovaGraphs);
    
    % Display the results
    disp("Multiple Comparisons of Means:");
    for j = 1:size(c, 1)
        group1 = groupNames(c(j, 1), :);
        group2 = groupNames(c(j, 2), :);
        lowerCI = c(j, 3);
        meanDiff = c(j, 4);
        upperCI = c(j, 5);
        pValue = c(j, 6);
        
        if pValue < pValThreshold
            sig = "Significant";
        else
            sig = "Not Significant";
        end
        comparisonString = "Comparison between " + group1 + " and " + group2 + ...
            ": Mean Difference = " + num2str(meanDiff) + ", CI = [" + ...
            num2str(lowerCI) + ", " + num2str(upperCI) + "], p-value = " + ...
            num2str(pValue) + " (" + sig + ")";
        disp(comparisonString);

    end
    disp("Left r^2: " + r2Values(i,1) + "; Right r^2: " + r2Values(i,2));
end
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
clear colNames colTypes conditionIndices currentData dataImport ...
    dataSeparatedByIV dependentVar i independentVar iv j outlierIndex ...
    outlierIndices participantID rtForCondition splitStr;
% graphing variables
clear currentColor fitLineLeft fitLineLeftCoefs fitLineRight ...
    fitLineRightCoefs leftXData leftYData nameForLegend r2Left r2Right ...
    residualsLeft residualsRight rightXData rightYData sseLeft sseRight ...
    sstLeft sstRight yObservedLeft yObservedRight yPredictedLeft ...
    yPredictedRight;
% anova / stats variables
%}






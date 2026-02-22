import React from 'react';

interface ActionFooterProps {
    status: string;
    onReset: () => void;
}

const ActionFooter: React.FC<ActionFooterProps> = ({ status, onReset }) => {
    return (
        <div className="sidebar-footer">
            <button className="full-reset-btn" onClick={onReset}>Reset to Defaults</button>
            <div className="status-bar">{status}</div>
        </div>
    );
};

export default ActionFooter;
